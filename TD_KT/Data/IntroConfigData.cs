using System.Data.SqlClient;
using TD_KT.Services;

namespace TD_KT.Data
{
    public class IntroConfigData
    {
        private readonly SqlDb _db;

        public IntroConfigData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public string GetContent(string key)
        {
            const string sql = "SELECT Content FROM dbo.IntroConfigs WHERE ConfigKey=@Key;";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Key", key ?? string.Empty);
                conn.Open();
                var val = cmd.ExecuteScalar();
                return val == null || val == System.DBNull.Value ? string.Empty : val.ToString();
            }
        }

        public void Upsert(string key, string configName, string content)
        {
            const string sql = @"
MERGE dbo.IntroConfigs AS target
USING (SELECT @Key AS ConfigKey) AS source
ON target.ConfigKey = source.ConfigKey
WHEN MATCHED THEN
  UPDATE SET ConfigName=@Name, Content=@Content
WHEN NOT MATCHED THEN
  INSERT (ConfigKey, ConfigName, Content) VALUES (@Key, @Name, @Content);";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Key", key ?? string.Empty);
                cmd.Parameters.AddWithValue("@Name", configName ?? string.Empty);
                cmd.Parameters.AddWithValue("@Content", content ?? string.Empty);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}