using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace TD_KT.Updater
{
    internal sealed class UpdateOptions
    {
        public string PackagePath { get; private set; }
        public string TargetDirectory { get; private set; }
        public string ApplicationFileName { get; private set; }
        public string Version { get; private set; }
        public string Sha256 { get; private set; }
        public int ProcessId { get; private set; }
        public string[] PreserveItems { get; private set; }
        public string DisplayName { get; private set; }

        public static UpdateOptions Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < args.Length; index++)
            {
                var key = args[index];
                if (!key.StartsWith("--", StringComparison.Ordinal))
                    continue;

                var value = index + 1 < args.Length ? args[index + 1] : string.Empty;
                if (!value.StartsWith("--", StringComparison.Ordinal))
                    index++;
                else
                    value = string.Empty;

                values[key.Substring(2)] = value;
            }

            var options = new UpdateOptions
            {
                PackagePath = GetRequired(values, "package"),
                TargetDirectory = GetRequired(values, "target"),
                ApplicationFileName = GetRequired(values, "application"),
                Version = GetRequired(values, "version"),
                Sha256 = GetOptional(values, "sha256"),
                ProcessId = ParseProcessId(GetRequired(values, "pid")),
                PreserveItems = SplitPreserveItems(GetOptional(values, "preserve")),
                DisplayName = GetOptional(values, "display-name")
            };

            options.TargetDirectory = Path.GetFullPath(options.TargetDirectory);
            options.PackagePath = Path.GetFullPath(options.PackagePath);

            if (!File.Exists(options.PackagePath))
                throw new FileNotFoundException("Không tìm thấy gói cập nhật.", options.PackagePath);

            if (!Directory.Exists(options.TargetDirectory))
                throw new DirectoryNotFoundException("Không tìm thấy thư mục cài đặt: " + options.TargetDirectory);

            if (string.IsNullOrWhiteSpace(options.DisplayName))
                options.DisplayName = "PHẦN MỀM THI ĐUA - KHEN THƯỞNG";

            return options;
        }

        private static string GetRequired(IDictionary<string, string> values, string key)
        {
            var value = GetOptional(values, key);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Thiếu tham số --" + key + ".");
            return value.Trim();
        }

        private static string GetOptional(IDictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;
        }

        private static int ParseProcessId(string value)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var processId) || processId <= 0)
                throw new ArgumentException("Tham số --pid không hợp lệ.");
            return processId;
        }

        private static string[] SplitPreserveItems(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new[] { "Files", "Logs", "Backup", "TD_KT.exe.config" };

            return value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
