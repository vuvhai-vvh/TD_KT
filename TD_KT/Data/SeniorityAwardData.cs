using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using TD_KT.Services;

#nullable enable

namespace TD_KT.Data
{
    public class SeniorityAwardRow
    {
        public int Id { get; set; }
        public bool IsSelected { get; set; }
        public int No { get; set; }

        public int Year { get; set; }
        public int SoldierId { get; set; }

        public string FullName { get; set; } = "";
        public string Rank { get; set; } = "";
        public string Position { get; set; } = "";
        public string OrgUnit { get; set; } = "";

        public DateTime? EnlistmentDate { get; set; }
        public string EnlistmentDateText => EnlistmentDate?.ToString("dd/MM/yyyy") ?? "";

        public int YearsOfService { get; set; }
        public string SuggestedReward { get; set; } = "";
        public string Status { get; set; } = "";

        // DB chưa có cột Note, để trống cho UI
        public string Note { get; set; } = "";

        public string PositionOrgUnit
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Position)) return OrgUnit ?? "";
                if (string.IsNullOrWhiteSpace(OrgUnit)) return Position ?? "";
                return $"{Position} / {OrgUnit}";
            }
        }
    }

    public class SeniorityAwardData
    {
        private readonly SqlDb _db;

        public SeniorityAwardData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<int> GetAvailableYears()
        {
            var years = new List<int>();
            const string sql = @"SELECT DISTINCT [Year] FROM dbo.SeniorityAwards ORDER BY [Year] DESC;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        years.Add(r.GetInt32(0));
                }
            }

            return years;
        }

        public int GenerateOrUpdateForYear(int year, DateTime asOfDate)
        {
            // Lấy dữ liệu quân nhân (đúng theo yêu cầu: lấy từ hồ sơ quân nhân)
            const string sql = @"
SELECT 
    s.Id AS SoldierId,
    s.FullName,
    ISNULL(r.Name,'') AS RankName,
    ISNULL(p.Name,'') AS PositionName,
    ISNULL(ou.Name,'') AS OrgUnitName,
    s.EnlistmentDate
FROM dbo.Soldiers s
LEFT JOIN dbo.Ranks r ON r.Id = s.RankId
LEFT JOIN dbo.Positions p ON p.Id = s.PositionId
LEFT JOIN dbo.OrgUnits ou ON ou.Id = s.OrgUnitId
WHERE s.EnlistmentDate IS NOT NULL;";

            var count = 0;

            using (var conn = _db.CreateConnection())
            {
                conn.Open();

                var soldiers = new List<(int SoldierId, string FullName, string Rank, string Position, string OrgUnit, DateTime EnlistmentDate)>();
                using (var cmd = new SqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var enlist = r.GetDateTime(r.GetOrdinal("EnlistmentDate"));
                        soldiers.Add((
                            r.GetInt32(r.GetOrdinal("SoldierId")),
                            SqlDb.GetString(r, "FullName"),
                            SqlDb.GetString(r, "RankName"),
                            SqlDb.GetString(r, "PositionName"),
                            SqlDb.GetString(r, "OrgUnitName"),
                            enlist
                        ));
                    }
                }

                foreach (var s in soldiers)
                {
                    var years = CalculateFullYears(s.EnlistmentDate, asOfDate);
                    var suggested = GetSuggestedReward(years);
                    if (string.IsNullOrWhiteSpace(suggested)) continue; // dưới 10 năm: không đưa vào danh sách

                    // Upsert theo (Year, SoldierId)
                    const string upsert = @"
IF EXISTS (SELECT 1 FROM dbo.SeniorityAwards WHERE [Year]=@Year AND SoldierId=@SoldierId)
BEGIN
    UPDATE dbo.SeniorityAwards
    SET 
        FullName=@FullName,
        Rank=@Rank,
        Position=@Position,
        OrgUnit=@OrgUnit,
        EnlistmentDate=@EnlistmentDate,
        YearsOfService=@YearsOfService,
        SuggestedReward=@SuggestedReward,
        Status = CASE WHEN Status = N'Đã trao' THEN Status ELSE N'Chưa trao' END
    WHERE [Year]=@Year AND SoldierId=@SoldierId;
END
ELSE
BEGIN
    INSERT INTO dbo.SeniorityAwards([Year], SoldierId, FullName, Rank, Position, OrgUnit, EnlistmentDate, YearsOfService, SuggestedReward, Status)
    VALUES(@Year, @SoldierId, @FullName, @Rank, @Position, @OrgUnit, @EnlistmentDate, @YearsOfService, @SuggestedReward, N'Chưa trao');
END";

                    using (var cmd = new SqlCommand(upsert, conn))
                    {
                        cmd.Parameters.AddWithValue("@Year", year);
                        cmd.Parameters.AddWithValue("@SoldierId", s.SoldierId);
                        cmd.Parameters.AddWithValue("@FullName", s.FullName ?? "");
                        cmd.Parameters.AddWithValue("@Rank", s.Rank ?? "");
                        cmd.Parameters.AddWithValue("@Position", s.Position ?? "");
                        cmd.Parameters.AddWithValue("@OrgUnit", s.OrgUnit ?? "");
                        cmd.Parameters.AddWithValue("@EnlistmentDate", s.EnlistmentDate.Date);
                        cmd.Parameters.AddWithValue("@YearsOfService", years);
                        cmd.Parameters.AddWithValue("@SuggestedReward", suggested);
                        cmd.ExecuteNonQuery();
                        count++;
                    }
                }
            }

            return count;
        }

        public List<SeniorityAwardRow> GetList(int? year, string? status)
        {
            var list = new List<SeniorityAwardRow>();

            const string sql = @"
SELECT 
    sa.Id,
    sa.[Year],
    sa.SoldierId,
    COALESCE(s.FullName, sa.FullName) AS FullName,
    COALESCE(NULLIF(r.Name, ''), sa.Rank, '') AS Rank,
    COALESCE(NULLIF(p.Name, ''), sa.Position, '') AS Position,
    COALESCE(NULLIF(ou.Name, ''), sa.OrgUnit, '') AS OrgUnit,
    sa.EnlistmentDate,
    sa.YearsOfService,
    ISNULL(sa.SuggestedReward,'') AS SuggestedReward,
    sa.Status
FROM dbo.SeniorityAwards sa
LEFT JOIN dbo.Soldiers s ON s.Id = sa.SoldierId
LEFT JOIN dbo.Ranks r ON r.Id = s.RankId
LEFT JOIN dbo.Positions p ON p.Id = s.PositionId
LEFT JOIN dbo.OrgUnits ou ON ou.Id = s.OrgUnitId
WHERE
    (@Year IS NULL OR sa.[Year] = @Year)
    AND (@Status IS NULL OR sa.Status = @Status)
ORDER BY sa.Status ASC, sa.YearsOfService DESC, sa.FullName ASC;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                var yearValue = year.HasValue ? (object)year.Value : DBNull.Value;
                var statusValue = string.IsNullOrWhiteSpace(status) || status == "Tất cả"
                    ? (object)DBNull.Value
                    : status;

                cmd.Parameters.Add("@Year", SqlDbType.Int).Value = yearValue;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = statusValue;

                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    var no = 1;
                    while (r.Read())
                    {
                        var enlist = r.IsDBNull(r.GetOrdinal("EnlistmentDate"))
                            ? (DateTime?)null
                            : r.GetDateTime(r.GetOrdinal("EnlistmentDate"));

                        list.Add(new SeniorityAwardRow
                        {
                            Id = SqlDb.GetInt(r, "Id"),
                            Year = SqlDb.GetInt(r, "Year"),
                            SoldierId = SqlDb.GetInt(r, "SoldierId"),
                            FullName = SqlDb.GetString(r, "FullName"),
                            Rank = SqlDb.GetString(r, "Rank"),
                            Position = SqlDb.GetString(r, "Position"),
                            OrgUnit = SqlDb.GetString(r, "OrgUnit"),
                            EnlistmentDate = enlist,
                            YearsOfService = SqlDb.GetInt(r, "YearsOfService"),
                            SuggestedReward = SqlDb.GetString(r, "SuggestedReward"),
                            Status = SqlDb.GetString(r, "Status"),
                            No = no++
                        });
                    }
                }
            }

            return list;
        }

        public void MarkAsAwarded(IEnumerable<int> ids)
        {
            var idList = (ids ?? new int[0]);
            var arr = new List<int>(idList);
            if (arr.Count == 0) return;

            // Dùng IN với parameter hóa đơn giản (số lượng nhỏ)
            using (var conn = _db.CreateConnection())
            {
                conn.Open();
                for (var i = 0; i < arr.Count; i++)
                {
                    using (var cmd = new SqlCommand("UPDATE dbo.SeniorityAwards SET Status = N'Đã trao' WHERE Id = @Id;", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", arr[i]);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static int CalculateFullYears(DateTime start, DateTime asOf)
        {
            var years = asOf.Year - start.Year;
            if (asOf.Date < start.Date.AddYears(years)) years--;
            return Math.Max(0, years);
        }

        private static string GetSuggestedReward(int yearsOfService)
        {
            if (yearsOfService >= 25) return "Quân kỳ Quyết thắng";
            if (yearsOfService >= 20) return "Chiến sĩ vẻ vang hạng Nhất";
            if (yearsOfService >= 15) return "Chiến sĩ vẻ vang hạng Nhì";
            if (yearsOfService >= 10) return "Chiến sĩ vẻ vang hạng Ba";
            return "";
        }
    }
}