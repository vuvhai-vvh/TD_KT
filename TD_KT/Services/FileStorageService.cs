using System;
using System.Diagnostics;
using System.IO;

namespace TD_KT.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _rootDir;
        private readonly string _relativeRoot;

        public FileStorageService()
            : this("DocumentProposals")
        {
        }

        public FileStorageService(string subFolder)
        {
            var safeFolder = string.IsNullOrWhiteSpace(subFolder) ? "DocumentProposals" : subFolder.Trim();
            _rootDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", safeFolder);
            _relativeRoot = Path.Combine("Files", safeFolder);
        }

        public string SaveToAppStorage(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
                throw new FileNotFoundException("Không tìm thấy file nguồn.", sourceFilePath);

            Directory.CreateDirectory(_rootDir);

            var ext = Path.GetExtension(sourceFilePath);
            var fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N") + ext;
            var destAbs = Path.Combine(_rootDir, fileName);

            File.Copy(sourceFilePath, destAbs, true);
            return Path.Combine(_relativeRoot, fileName);
        }

        public string ResolvePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return string.Empty;
            if (Path.IsPathRooted(filePath)) return filePath;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
        }

        public void OpenWithDefaultApp(string filePath)
        {
            var abs = ResolvePath(filePath);
            if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                throw new FileNotFoundException("Không tìm thấy file.", abs);

            Process.Start(new ProcessStartInfo(abs) { UseShellExecute = true });
        }

        public void CopyTo(string fromFilePath, string toFilePath)
        {
            var src = ResolvePath(fromFilePath);
            if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
                throw new FileNotFoundException("Không tìm thấy file nguồn.", src);

            File.Copy(src, toFilePath, true);
        }

        public void OpenBytesWithDefaultApp(byte[] fileData, string fileName)
        {
            if (fileData == null || fileData.Length == 0)
                throw new InvalidDataException("File trong cơ sở dữ liệu không có nội dung.");

            var safeName = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "document" : fileName);
            var previewDir = Path.Combine(Path.GetTempPath(), "TD_KT", "FilePreview");
            Directory.CreateDirectory(previewDir);

            var previewPath = Path.Combine(previewDir, Guid.NewGuid().ToString("N") + "_" + safeName);
            File.WriteAllBytes(previewPath, fileData);
            Process.Start(new ProcessStartInfo(previewPath) { UseShellExecute = true });
        }

        public void SaveBytesTo(byte[] fileData, string toFilePath)
        {
            if (fileData == null || fileData.Length == 0)
                throw new InvalidDataException("File trong cơ sở dữ liệu không có nội dung.");
            if (string.IsNullOrWhiteSpace(toFilePath))
                throw new ArgumentException("Đường dẫn lưu file không hợp lệ.", nameof(toFilePath));

            File.WriteAllBytes(toFilePath, fileData);
        }
    }
}
