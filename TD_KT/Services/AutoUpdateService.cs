using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using TD_KT.Models;

namespace TD_KT.Services
{
    public sealed class AutoUpdateResult
    {
        public bool UpdateStarted { get; set; }
        public bool BlockStartup { get; set; }
        public string Message { get; set; }
    }

    public sealed class AutoUpdateService
    {
        private const string UpdaterFileName = "TD_KT.Updater.exe";

        public async Task<AutoUpdateResult> CheckAndStartUpdateAsync(IProgress<string> progress)
        {
            if (!ReadBooleanSetting("AutoUpdate.Enabled", true))
                return new AutoUpdateResult();

            var manifestLocation = (ConfigurationManager.AppSettings["AutoUpdate.ManifestPath"] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(manifestLocation))
                return new AutoUpdateResult();

            UpdateManifest detectedManifest = null;
            Version detectedVersion = null;
            var updateWasDetected = false;

            try
            {
                progress?.Report("Đang kiểm tra phiên bản mới nhất...");
                var manifestJson = await ReadTextAsync(manifestLocation).ConfigureAwait(true);
                var manifest = new JavaScriptSerializer().Deserialize<UpdateManifest>(manifestJson);
                detectedManifest = manifest;

                ValidateManifest(manifest);

                var currentVersion = AppInfo.Version;
                if (!System.Version.TryParse(manifest.version, out var serverVersion))
                    throw new InvalidDataException("Phiên bản trong manifest.json không hợp lệ.");

                if (serverVersion <= currentVersion)
                    return new AutoUpdateResult();

                detectedVersion = serverVersion;
                updateWasDetected = true;

                if (!manifest.mandatory)
                {
                    var message = $"Đã có phiên bản {serverVersion}. Bạn có muốn cập nhật ngay không?";
                    if (!string.IsNullOrWhiteSpace(manifest.description))
                        message += $"\n\nNội dung cập nhật:\n{manifest.description.Trim()}";

                    var choice = MessageBox.Show(
                        message,
                        "Cập nhật phần mềm",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (choice != MessageBoxResult.Yes)
                        return new AutoUpdateResult();
                }

                progress?.Report($"Đang chuẩn bị bản cập nhật {serverVersion}...");

                var packageLocation = ResolvePackageLocation(manifestLocation, manifest.package);
                var localPackage = await CopyPackageToTempAsync(packageLocation, serverVersion.ToString()).ConfigureAwait(true);
                var updaterPath = PrepareUpdaterExecutable();

                progress?.Report("Đang khởi chạy trình cập nhật...");

                var startInfo = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = BuildUpdaterArguments(localPackage, manifest, serverVersion),
                    WorkingDirectory = Path.GetDirectoryName(updaterPath),
                    UseShellExecute = true
                };

                if (ReadBooleanSetting("AutoUpdate.RequireAdministrator", false))
                    startInfo.Verb = "runas";

                Process.Start(startInfo);

                return new AutoUpdateResult
                {
                    UpdateStarted = true,
                    BlockStartup = true,
                    Message = $"Đang cập nhật lên phiên bản {serverVersion}."
                };
            }
            catch (Exception ex)
            {
                var message = "Không thể kiểm tra hoặc cài đặt bản cập nhật: " + GetFriendlyMessage(ex);
                var mustBlockStartup = updateWasDetected
                    && detectedManifest != null
                    && detectedManifest.mandatory;

                progress?.Report(mustBlockStartup
                    ? "Cập nhật bắt buộc không thành công."
                    : "Không thể cập nhật. Đang mở màn hình đăng nhập...");

                if (mustBlockStartup && detectedVersion != null)
                {
                    message += $"\n\nPhiên bản {detectedVersion} là bản cập nhật bắt buộc. "
                               + "Vui lòng kiểm tra kết nối máy chủ hoặc liên hệ quản trị viên.";
                }

                return new AutoUpdateResult
                {
                    UpdateStarted = false,
                    BlockStartup = mustBlockStartup,
                    Message = message
                };
            }
        }

        private static void ValidateManifest(UpdateManifest manifest)
        {
            if (manifest == null)
                throw new InvalidDataException("Không đọc được manifest.json.");
            if (string.IsNullOrWhiteSpace(manifest.version))
                throw new InvalidDataException("manifest.json chưa có trường version.");
            if (string.IsNullOrWhiteSpace(manifest.package))
                throw new InvalidDataException("manifest.json chưa có trường package.");
        }

        private static async Task<string> ReadTextAsync(string location)
        {
            if (IsHttpLocation(location))
            {
                using (var client = CreateWebClient())
                    return await client.DownloadStringTaskAsync(location).ConfigureAwait(false);
            }

            return await Task.Run(() => File.ReadAllText(location)).ConfigureAwait(false);
        }

        private static async Task<string> CopyPackageToTempAsync(string packageLocation, string version)
        {
            var updateFolder = Path.Combine(
                Path.GetTempPath(),
                "TD-KT",
                "Updates",
                SanitizeFileName(version));

            Directory.CreateDirectory(updateFolder);

            var packageName = GetPackageFileName(packageLocation);
            var localPackage = Path.Combine(updateFolder, packageName);

            if (File.Exists(localPackage))
                File.Delete(localPackage);

            if (IsHttpLocation(packageLocation))
            {
                using (var client = CreateWebClient())
                    await client.DownloadFileTaskAsync(packageLocation, localPackage).ConfigureAwait(false);
            }
            else
            {
                await Task.Run(() => File.Copy(packageLocation, localPackage, true)).ConfigureAwait(false);
            }

            return localPackage;
        }

        private static WebClient CreateWebClient()
        {
            var client = new WebClient();
            client.Headers[HttpRequestHeader.UserAgent] = "TD-KT AutoUpdater/" + AppInfo.VersionText;
            return client;
        }

        private static string ResolvePackageLocation(string manifestLocation, string package)
        {
            package = package.Trim();

            if (IsHttpLocation(package) || Path.IsPathRooted(package))
                return package;

            if (IsHttpLocation(manifestLocation))
            {
                var baseUri = new Uri(manifestLocation, UriKind.Absolute);
                return new Uri(baseUri, package).AbsoluteUri;
            }

            var manifestDirectory = Path.GetDirectoryName(manifestLocation);
            if (string.IsNullOrWhiteSpace(manifestDirectory))
                throw new InvalidDataException("Không xác định được thư mục chứa manifest.json.");

            return Path.Combine(manifestDirectory, package);
        }

        private static string PrepareUpdaterExecutable()
        {
            var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, UpdaterFileName);
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Không tìm thấy trình cập nhật " + UpdaterFileName + ".", sourcePath);

            var tempFolder = Path.Combine(
                Path.GetTempPath(),
                "TD-KT",
                "Updater",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(tempFolder);

            var destinationPath = Path.Combine(tempFolder, UpdaterFileName);
            File.Copy(sourcePath, destinationPath, true);
            return destinationPath;
        }

        private static string BuildUpdaterArguments(string localPackage, UpdateManifest manifest, Version version)
        {
            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var targetDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var preserveItems = ConfigurationManager.AppSettings["AutoUpdate.PreserveItems"]
                ?? "Files;Logs;Backup;TD_KT.exe.config";

            return string.Join(" ", new[]
            {
                "--package", Quote(localPackage),
                "--target", Quote(targetDirectory),
                "--application", Quote(Path.GetFileName(applicationPath)),
                "--version", Quote(version.ToString()),
                "--sha256", Quote(manifest.sha256 ?? string.Empty),
                "--pid", Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture),
                "--preserve", Quote(preserveItems),
                "--display-name", Quote(AppInfo.DisplayName)
            });
        }

        private static string GetPackageFileName(string packageLocation)
        {
            if (IsHttpLocation(packageLocation))
            {
                var uri = new Uri(packageLocation, UriKind.Absolute);
                var name = Path.GetFileName(uri.LocalPath);
                return string.IsNullOrWhiteSpace(name) ? "TD_KT_Update.zip" : name;
            }

            var fileName = Path.GetFileName(packageLocation);
            return string.IsNullOrWhiteSpace(fileName) ? "TD_KT_Update.zip" : fileName;
        }

        private static bool IsHttpLocation(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return false;

            return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                   || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ReadBooleanSetting(string key, bool defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        private static string Quote(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string SanitizeFileName(string value)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                value = value.Replace(invalidChar, '_');
            return value;
        }

        private static string GetFriendlyMessage(Exception exception)
        {
            if (exception is WebException webException && webException.Response == null)
                return "không kết nối được máy chủ cập nhật.";
            if (exception is FileNotFoundException)
                return exception.Message;
            if (exception is DirectoryNotFoundException)
                return "không tìm thấy thư mục cập nhật trên máy chủ.";

            return exception.Message;
        }
    }
}
