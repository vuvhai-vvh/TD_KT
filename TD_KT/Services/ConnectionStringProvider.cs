using System;
using System.Configuration;

namespace TD_KT.Services
{
    public class ConnectionStringProvider : IConnectionStringProvider
    {
        private readonly string _name;
        private const string OverrideKey = "TD_KT_CONNECTION_STRING";

        public ConnectionStringProvider(string name = "Database")
        {
            
            _name = string.IsNullOrWhiteSpace(name) ? "Database" : name;
        }

        public string GetConnectionString()
        {
            // 0) ưu tiên override qua biến môi trường hoặc appSettings
            var overrideValue = Environment.GetEnvironmentVariable(OverrideKey);
            if (string.IsNullOrWhiteSpace(overrideValue))
                overrideValue = ConfigurationManager.AppSettings[OverrideKey];

            if (!string.IsNullOrWhiteSpace(overrideValue))
                return overrideValue;

            // 1) thử lấy đúng tên được yêu cầu
            var cs = ConfigurationManager.ConnectionStrings[_name]?.ConnectionString;
            if (!string.IsNullOrWhiteSpace(cs))
                return cs;

            // 2) fallback về "Database"
            cs = ConfigurationManager.ConnectionStrings["Database"]?.ConnectionString;
            if (!string.IsNullOrWhiteSpace(cs))
                return cs;

            // 3) nếu vẫn không có thì báo lỗi rõ ràng
            throw new InvalidOperationException(
                $"Hãy kiểm tra <connectionStrings> trong App.config / TD-KT.exe.config " +
                $"hoặc đặt {OverrideKey} trong appSettings/biến môi trường."
            );
        }
    }
}
