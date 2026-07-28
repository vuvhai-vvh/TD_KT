using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using TD_KT.Services;

#nullable enable

namespace TD_KT.Data
{
    public class MedalRecordDataRow
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string OrgUnit { get; set; } = string.Empty;
        public string TitleName { get; set; } = string.Empty;
        public string DecisionNumber { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }

    public class MedalRecordData
    {
        private readonly SqlDb _db;

        public MedalRecordData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<MedalRecordDataRow> GetList()
        {
            var list = new List<MedalRecordDataRow>();
            const string sql = @"
SELECT Id, FullName, Rank, Position, OrgUnit, TitleName, DecisionNumber, Note, CreatedAt
FROM dbo.MedalRecords
ORDER BY CreatedAt DESC, Id DESC;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new MedalRecordDataRow
                        {
                            Id = SqlDb.GetInt(r, "Id"),
                            FullName = SqlDb.GetString(r, "FullName"),
                            Rank = SqlDb.GetString(r, "Rank"),
                            Position = SqlDb.GetString(r, "Position"),
                            OrgUnit = SqlDb.GetString(r, "OrgUnit"),
                            TitleName = SqlDb.GetString(r, "TitleName"),
                            DecisionNumber = SqlDb.GetString(r, "DecisionNumber"),
                            Note = SqlDb.GetString(r, "Note"),
                            CreatedAt = r.IsDBNull(r.GetOrdinal("CreatedAt"))
                                ? (DateTime?)null
                                : r.GetDateTime(r.GetOrdinal("CreatedAt"))
                        });
                    }
                }
            }

            return list;
        }

        public int Insert(MedalRecordDataRow record)
        {
            const string sql = @"
INSERT INTO dbo.MedalRecords(FullName, Rank, Position, OrgUnit, TitleName, DecisionNumber, Note, CreatedAt)
VALUES(@FullName, @Rank, @Position, @OrgUnit, @TitleName, @DecisionNumber, @Note, @CreatedAt);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@FullName", record.FullName ?? string.Empty);
                cmd.Parameters.AddWithValue("@Rank", record.Rank ?? string.Empty);
                cmd.Parameters.AddWithValue("@Position", record.Position ?? string.Empty);
                cmd.Parameters.AddWithValue("@OrgUnit", record.OrgUnit ?? string.Empty);
                cmd.Parameters.AddWithValue("@TitleName", record.TitleName ?? string.Empty);
                cmd.Parameters.AddWithValue("@DecisionNumber", record.DecisionNumber ?? string.Empty);
                cmd.Parameters.AddWithValue("@Note", (object?)record.Note ?? DBNull.Value);
                cmd.Parameters.Add("@CreatedAt", SqlDbType.DateTime).Value = record.CreatedAt ?? DateTime.Now;

                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
        }

        public void Update(MedalRecordDataRow record)
        {
            const string sql = @"
UPDATE dbo.MedalRecords
SET FullName = @FullName,
    Rank = @Rank,
    Position = @Position,
    OrgUnit = @OrgUnit,
    TitleName = @TitleName,
    DecisionNumber = @DecisionNumber,
    Note = @Note
WHERE Id = @Id;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", record.Id);
                cmd.Parameters.AddWithValue("@FullName", record.FullName ?? string.Empty);
                cmd.Parameters.AddWithValue("@Rank", record.Rank ?? string.Empty);
                cmd.Parameters.AddWithValue("@Position", record.Position ?? string.Empty);
                cmd.Parameters.AddWithValue("@OrgUnit", record.OrgUnit ?? string.Empty);
                cmd.Parameters.AddWithValue("@TitleName", record.TitleName ?? string.Empty);
                cmd.Parameters.AddWithValue("@DecisionNumber", record.DecisionNumber ?? string.Empty);
                cmd.Parameters.AddWithValue("@Note", (object?)record.Note ?? DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            const string sql = @"DELETE FROM dbo.MedalRecords WHERE Id = @Id;";

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