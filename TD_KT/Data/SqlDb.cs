using System;
using System.Data;
using System.Data.SqlClient;
using TD_KT.Services;

namespace TD_KT.Data
{
    /// <summary>
    /// Base helper for SQL access (ADO.NET). All CRUD SQL stays in Data layer.
    /// </summary>
    public class SqlDb
    {
        private readonly string _connectionString;

        public SqlDb(IConnectionStringProvider connectionStringProvider)
        {
            _connectionString = connectionStringProvider?.GetConnectionString() ?? string.Empty;
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public static object DbNullIfEmpty(string value)
            => string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value;

        public static object DbNullIfZero(int value)
            => value == 0 ? (object)DBNull.Value : value;

        public static int GetInt(IDataRecord r, string name, int defaultValue = 0)
        {
            var ord = r.GetOrdinal(name);
            return r.IsDBNull(ord) ? defaultValue : r.GetInt32(ord);
        }

        public static int? GetNullableInt(IDataRecord r, string name)
        {
            var ord = r.GetOrdinal(name);
            return r.IsDBNull(ord) ? (int?)null : r.GetInt32(ord);
        }

        public static string GetString(IDataRecord r, string name, string defaultValue = "")
        {
            var ord = r.GetOrdinal(name);
            return r.IsDBNull(ord) ? defaultValue : r.GetString(ord);
        }

        public static bool GetBool(IDataRecord r, string name, bool defaultValue = false)
        {
            var ord = r.GetOrdinal(name);
            return r.IsDBNull(ord) ? defaultValue : r.GetBoolean(ord);
        }

        public static DateTime? GetDateTime(IDataRecord r, string name)
        {
            var ord = r.GetOrdinal(name);
            return r.IsDBNull(ord) ? (DateTime?)null : r.GetDateTime(ord);
        }
    }
}