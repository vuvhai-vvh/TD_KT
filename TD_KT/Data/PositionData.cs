using System.Collections.Generic;
using System.Data.SqlClient;
using TD_KT.Models;
using TD_KT.Services;
using TD_KT.ViewModels;

namespace TD_KT.Data
{
    public class PositionData
    {
        private readonly SqlDb _db;

        public PositionData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<Position> GetAll()
        {
            var result = new List<Position>();
            const string sql = "SELECT Id, Code, Name, Note FROM dbo.Positions ORDER BY Name;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new Position
                        {
                            Id = SqlDb.GetInt(r, "Id"),
                            Code = SqlDb.GetString(r, "Code"),
                            Name = SqlDb.GetString(r, "Name"),
                            Note = SqlDb.GetString(r, "Note")
                        });
                    }
                }
            }

            return result;
        }

        public List<PositionDisplayModel> GetAllDisplay()
        {
            var result = new List<PositionDisplayModel>();
            const string sql = "SELECT Id, Code, Name FROM dbo.Positions ORDER BY Name;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    var stt = 1;
                    while (r.Read())
                    {
                        result.Add(new PositionDisplayModel
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
            const string sql = @"
INSERT INTO dbo.Positions(Code, Name)
VALUES(@Code, @Name);";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Code", (object)(code ?? string.Empty));
                cmd.Parameters.AddWithValue("@Name", (object)(name ?? string.Empty));
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(int id, string code, string name)
        {
            const string sql = @"
UPDATE dbo.Positions
SET Code=@Code, Name=@Name
WHERE Id=@Id;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Code", (object)(code ?? string.Empty));
                cmd.Parameters.AddWithValue("@Name", (object)(name ?? string.Empty));
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            const string sql = "DELETE FROM dbo.Positions WHERE Id=@Id;";
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