using System.Collections.Generic;
using System.Data.SqlClient;
using TD_KT.Models;
using TD_KT.Services;

namespace TD_KT.Data
{
    public class LocationData
    {
        private readonly SqlDb _db;

        public LocationData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<Location> GetByLevel(int level, int? parentId)
        {
            var result = new List<Location>();
            const string sql = @"
SELECT Id, Name, Level, ParentId
FROM dbo.Locations
WHERE Level=@Level AND ((@ParentId IS NULL AND ParentId IS NULL) OR ParentId=@ParentId)
ORDER BY Name;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Level", level);
                cmd.Parameters.AddWithValue("@ParentId", (object)parentId ?? System.DBNull.Value);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new Location
                        {
                            Id = SqlDb.GetInt(r, "Id"),
                            Name = SqlDb.GetString(r, "Name"),
                            Level = SqlDb.GetInt(r, "Level"),
                            ParentId = SqlDb.GetNullableInt(r, "ParentId")
                        });
                    }
                }
            }
            return result;
        }

        // Load theo ParentId (đúng yêu cầu):
        // - parentId = NULL  => lấy các dòng cấp gốc (ParentId IS NULL)
        // - parentId = ...   => lấy các dòng con (ParentId = parentId)
        public List<Location> GetByParent(int? parentId)
        {
            var result = new List<Location>();
            const string sql = @"
SELECT Id, Name, Level, ParentId
FROM dbo.Locations
WHERE ((@ParentId IS NULL AND ParentId IS NULL) OR ParentId=@ParentId)
ORDER BY Name;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ParentId", (object)parentId ?? System.DBNull.Value);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new Location
                        {
                            Id = SqlDb.GetInt(r, "Id"),
                            Name = SqlDb.GetString(r, "Name"),
                            Level = SqlDb.GetInt(r, "Level"),
                            ParentId = SqlDb.GetNullableInt(r, "ParentId")
                        });
                    }
                }
            }
            return result;
        }

        public void Insert(string name, int level, int? parentId)
        {
            const string sql = "INSERT INTO dbo.Locations(Name, Level, ParentId) VALUES(@Name, @Level, @ParentId);";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Name", name ?? string.Empty);
                cmd.Parameters.AddWithValue("@Level", level);
                cmd.Parameters.AddWithValue("@ParentId", (object)parentId ?? System.DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(int id, string name)
        {
            const string sql = "UPDATE dbo.Locations SET Name=@Name WHERE Id=@Id;";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Name", name ?? string.Empty);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            const string sql = "DELETE FROM dbo.Locations WHERE Id=@Id;";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}