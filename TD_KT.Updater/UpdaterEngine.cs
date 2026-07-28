using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace TD_KT.Updater
{
    internal sealed class UpdaterEngine
    {
        private readonly UpdateOptions _options;
        private readonly Action<int, string> _report;

        public UpdaterEngine(UpdateOptions options, Action<int, string> report)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _report = report ?? ((percent, status) => { });
        }

        public void Run()
        {
            var workingRoot = Path.Combine(Path.GetTempPath(), "TD-KT", "Apply", Guid.NewGuid().ToString("N"));
            var extractDirectory = Path.Combine(workingRoot, "Extracted");
            var backupDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TD-KT",
                "Backups",
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + SanitizeFileName(_options.Version));

            var replacedFiles = new List<ReplacedFile>();
            var createdFiles = new List<string>();

            try
            {
                Directory.CreateDirectory(workingRoot);
                Directory.CreateDirectory(extractDirectory);
                Directory.CreateDirectory(backupDirectory);

                _report(5, "Đang chờ phần mềm đóng...");
                WaitForApplicationToExit();

                _report(15, "Đang kiểm tra gói cập nhật...");
                VerifyPackageHash();

                _report(28, "Đang giải nén bản cập nhật...");
                ExtractZipSafely(_options.PackagePath, extractDirectory);

                var packageRoot = FindPackageRoot(extractDirectory);
                var sourceFiles = Directory.GetFiles(packageRoot, "*", SearchOption.AllDirectories)
                    .Where(path => !IsPreserved(GetRelativePath(packageRoot, path)))
                    .ToList();

                if (sourceFiles.Count == 0)
                    throw new InvalidDataException("Gói cập nhật không có file chương trình để cài đặt.");

                var appInPackage = Path.Combine(packageRoot, _options.ApplicationFileName);
                if (!File.Exists(appInPackage))
                    throw new InvalidDataException("Gói cập nhật không chứa " + _options.ApplicationFileName + ".");

                _report(40, "Đang sao lưu phiên bản hiện tại...");

                for (var index = 0; index < sourceFiles.Count; index++)
                {
                    var sourceFile = sourceFiles[index];
                    var relativePath = GetRelativePath(packageRoot, sourceFile);
                    var destinationFile = GetSafeDestinationPath(_options.TargetDirectory, relativePath);
                    var backupFile = Path.Combine(backupDirectory, relativePath);

                    if (File.Exists(destinationFile))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(backupFile));
                        File.Copy(destinationFile, backupFile, true);
                        replacedFiles.Add(new ReplacedFile(destinationFile, backupFile));
                    }
                    else
                    {
                        createdFiles.Add(destinationFile);
                    }

                    var percent = 40 + (int)Math.Round(((index + 1d) / sourceFiles.Count) * 45d);
                    _report(percent, "Đang cập nhật: " + relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                    File.Copy(sourceFile, destinationFile, true);
                }

                _report(90, "Đang kiểm tra file sau cập nhật...");
                var installedApplication = Path.Combine(_options.TargetDirectory, _options.ApplicationFileName);
                if (!File.Exists(installedApplication))
                    throw new FileNotFoundException("Không tìm thấy file chương trình sau khi cập nhật.", installedApplication);

                _report(96, "Đang mở lại phần mềm...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = installedApplication,
                    WorkingDirectory = _options.TargetDirectory,
                    UseShellExecute = true
                });

                _report(100, "Cập nhật hoàn tất.");

                TryDeleteFile(_options.PackagePath);
                TryDeleteDirectory(workingRoot);
            }
            catch
            {
                _report(85, "Cập nhật gặp lỗi. Đang khôi phục phiên bản cũ...");
                Rollback(replacedFiles, createdFiles);
                throw;
            }
        }

        private void WaitForApplicationToExit()
        {
            try
            {
                using (var process = Process.GetProcessById(_options.ProcessId))
                {
                    if (!process.WaitForExit(60000))
                        throw new TimeoutException("Phần mềm chưa đóng sau 60 giây. Vui lòng đóng phần mềm và thử lại.");
                }
            }
            catch (ArgumentException)
            {
                // Process đã đóng trước khi Updater bắt đầu kiểm tra.
            }
        }

        private void VerifyPackageHash()
        {
            if (string.IsNullOrWhiteSpace(_options.Sha256))
                return;

            using (var stream = File.OpenRead(_options.PackagePath))
            using (var sha256 = SHA256.Create())
            {
                var actualHash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
                var expectedHash = _options.Sha256.Trim().Replace("-", string.Empty);

                if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Mã SHA-256 của gói cập nhật không khớp. Gói cập nhật có thể bị lỗi.");
            }
        }

        private static void ExtractZipSafely(string zipPath, string destinationDirectory)
        {
            var destinationRoot = Path.GetFullPath(destinationDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    var entryPath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, entryPath));

                    if (!destinationPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Gói cập nhật chứa đường dẫn không an toàn: " + entry.FullName);

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    entry.ExtractToFile(destinationPath, true);
                }
            }
        }

        private string FindPackageRoot(string extractDirectory)
        {
            if (File.Exists(Path.Combine(extractDirectory, _options.ApplicationFileName)))
                return extractDirectory;

            var topDirectories = Directory.GetDirectories(extractDirectory);
            if (topDirectories.Length == 1 && File.Exists(Path.Combine(topDirectories[0], _options.ApplicationFileName)))
                return topDirectories[0];

            throw new InvalidDataException(
                "Không xác định được thư mục gốc của gói cập nhật. Hãy đặt "
                + _options.ApplicationFileName
                + " ở thư mục gốc của file ZIP.");
        }

        private bool IsPreserved(string relativePath)
        {
            var normalizedPath = NormalizeRelativePath(relativePath);

            foreach (var item in _options.PreserveItems)
            {
                var normalizedItem = NormalizeRelativePath(item);
                if (string.IsNullOrWhiteSpace(normalizedItem))
                    continue;

                if (normalizedPath.Equals(normalizedItem, StringComparison.OrdinalIgnoreCase)
                    || normalizedPath.StartsWith(normalizedItem + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string NormalizeRelativePath(string path)
        {
            return (path ?? string.Empty)
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                .Trim()
                .TrimStart(Path.DirectorySeparatorChar)
                .TrimEnd(Path.DirectorySeparatorChar);
        }

        private static string GetRelativePath(string rootDirectory, string fullPath)
        {
            var root = Path.GetFullPath(rootDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var file = Path.GetFullPath(fullPath);

            if (!file.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("File không nằm trong thư mục gói cập nhật.");

            return file.Substring(root.Length);
        }

        private static string GetSafeDestinationPath(string targetDirectory, string relativePath)
        {
            var targetRoot = Path.GetFullPath(targetDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var destination = Path.GetFullPath(Path.Combine(targetDirectory, relativePath));

            if (!destination.StartsWith(targetRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Đường dẫn file cập nhật không hợp lệ: " + relativePath);

            return destination;
        }

        private static void Rollback(IEnumerable<ReplacedFile> replacedFiles, IEnumerable<string> createdFiles)
        {
            foreach (var createdFile in createdFiles.Reverse())
                TryDeleteFile(createdFile);

            foreach (var item in replacedFiles.Reverse())
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(item.DestinationPath));
                    File.Copy(item.BackupPath, item.DestinationPath, true);
                }
                catch
                {
                    // Tiếp tục khôi phục các file còn lại.
                }
            }
        }

        private static string SanitizeFileName(string value)
        {
            value = value ?? "unknown";
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                value = value.Replace(invalidChar, '_');
            return value;
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch
            {
            }
        }

        private sealed class ReplacedFile
        {
            public ReplacedFile(string destinationPath, string backupPath)
            {
                DestinationPath = destinationPath;
                BackupPath = backupPath;
            }

            public string DestinationPath { get; }
            public string BackupPath { get; }
        }
    }
}
