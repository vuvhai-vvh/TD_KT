using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TD_KT.Models;
using TD_KT.Services;
using TD_KT.ViewModels;

namespace TD_KT.Data
{
    public class RewardFormData
    {
        private readonly SqlDb _db;

        public RewardFormData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<RewardForm> GetAll()
        {
            var result = new List<RewardForm>();
            const string sql = @"SELECT rf.Id, rf.Code, rf.Name, rf.Note
FROM dbo.RewardForms rf
ORDER BY rf.Name;";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new RewardForm
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

        public List<RewardFormDisplayModel> GetAllDisplay()
        {
            var result = new List<RewardFormDisplayModel>();
            const string sql = @"SELECT rf.Id, rf.Code, rf.Name
FROM dbo.RewardForms rf
ORDER BY rf.Name;";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    var stt = 1;
                    while (r.Read())
                    {
                        result.Add(new RewardFormDisplayModel
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

        private int GetDefaultIssuingLevelId(SqlConnection conn)
        {
            const string sql = @"SELECT TOP 1 Id
FROM dbo.IssuingLevels
WHERE IsActive = 1
ORDER BY Id;";
            const string fallbackSql = @"SELECT TOP 1 Id FROM dbo.IssuingLevels ORDER BY Id;";

            using (var cmd = new SqlCommand(sql, conn))
            {
                var value = cmd.ExecuteScalar();
                if (value != null && value != DBNull.Value)
                    return Convert.ToInt32(value);
            }

            using (var cmd = new SqlCommand(fallbackSql, conn))
            {
                var value = cmd.ExecuteScalar();
                if (value != null && value != DBNull.Value)
                    return Convert.ToInt32(value);
            }

            throw new InvalidOperationException("Không tìm thấy Cấp ban hành mặc định để lưu hình thức khen thưởng.");
        }

        public void Insert(string code, string name)
        {
            const string sql = "INSERT INTO dbo.RewardForms(Code, Name, IssuingLevelId) VALUES(@Code, @Name, @IssuingLevelId);";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@Code", code ?? string.Empty);
                cmd.Parameters.AddWithValue("@Name", name ?? string.Empty);
                cmd.Parameters.AddWithValue("@IssuingLevelId", GetDefaultIssuingLevelId(conn));
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(int id, string code, string name)
        {
            const string sql = "UPDATE dbo.RewardForms SET Code=@Code, Name=@Name WHERE Id=@Id;";
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
            const string sql = "DELETE FROM dbo.RewardForms WHERE Id=@Id;";
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