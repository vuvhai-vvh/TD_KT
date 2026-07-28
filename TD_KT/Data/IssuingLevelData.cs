using System.Collections.Generic;
using System.Data.SqlClient;
using TD_KT.Services;

namespace TD_KT.Data
{
    /// <summary>
    /// CRUD cho danh mục: Cấp ban hành (dbo.IssuingLevels).
    /// Bám sát cấu trúc RewardFormData.
    /// </summary>
    public class IssuingLevelData
    {
        private readonly SqlDb _db;

        public IssuingLevelData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<IssuingLevelDisplayModel> GetAllDisplay(bool onlyActive = true)
        {
            var result = new List<IssuingLevelDisplayModel>();
            var sql = onlyActive
                ? "SELECT Id, Code, Name FROM dbo.IssuingLevels WHERE IsActive = 1 ORDER BY Name;"
                : "SELECT Id, Code, Name FROM dbo.IssuingLevels ORDER BY Name;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    var stt = 1;
                    while (r.Read())
                    {
                        result.Add(new IssuingLevelDisplayModel
                        {
                            Stt = stt++,
                            Id = SqlDb.GetInt(r, "Id"),
                            Code = SqlDb.GetString(r, "Code"),
                            Name = SqlDb.GetString(r, "Name")
                        });
                    }
                }
            }

            return result;
        }

        public void Insert(string code, string name)
        {
            const string sql = "INSERT INTO dbo.IssuingLevels(Code, Name, IsActive, CreatedAt) VALUES(@Code, @Name, 1, GETDATE());";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Code", code ?? string.Empty);
                cmd.Parameters.AddWithValue("@Name", name ?? string.Empty);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(int id, string code, string name)
        {
            const string sql = "UPDATE dbo.IssuingLevels SET Code=@Code, Name=@Name WHERE Id=@Id;";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Code", code ?? string.Empty);
                cmd.Parameters.AddWithValue("@Name", name ?? string.Empty);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            const string sql = "DELETE FROM dbo.IssuingLevels WHERE Id=@Id;";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Model hiển thị cho DataGrid danh mục Cấp ban hành.
    /// </summary>
    public class IssuingLevelDisplayModel
    {
        public int Stt { get; set; }
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
}