using System;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace TD_KT.Services
{
    public static class AppInfo
    {
        private const string DefaultDisplayName = "PHẦN MỀM THI ĐUA - KHEN THƯỞNG";

        public static string DisplayName
        {
            get
            {
                var configuredName = ConfigurationManager.AppSettings["Application.DisplayName"];
                return string.IsNullOrWhiteSpace(configuredName)
                    ? DefaultDisplayName
                    : configuredName.Trim();
            }
        }

        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public static string VersionText
        {
            get
            {
                var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
                return string.IsNullOrWhiteSpace(fileVersion) ? Version.ToString() : fileVersion;
            }
        }
    }
}
