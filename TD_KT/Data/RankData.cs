using System.Collections.Generic;
using System.Data.SqlClient;
using TD_KT.Models;
using TD_KT.Services;

namespace TD_KT.Data
{
    public class RankData
    {
        private readonly SqlDb _db;

        public RankData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<Rank> GetAll()
        {
            var result = new List<Rank>();
            const string sql = "SELECT Id, Name FROM dbo.Ranks ORDER BY Id;";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new Rank
                        {
                            Id = SqlDb.GetInt(r, "Id"),
                            Name = SqlDb.GetString(r, "Name")
                        });
                    }
                }
            }
            return result;
        }
    }
}