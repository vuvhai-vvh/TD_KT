using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using TD_KT.Services;

#nullable enable

namespace TD_KT.Data
{
    public class UnitScoreSummaryRow
    {
        public int OrgUnitId { get; set; }
        public string OrgUnitName { get; set; } = "";

        public int Week { get; set; }

        public decimal? ScoreArea1 { get; set; }
        public decimal? ScoreArea2 { get; set; }
        public decimal? ScoreArea3 { get; set; }
        public decimal? ScoreArea4 { get; set; }

        // Lưu theo ý nghĩa mới: "Tổng điểm của tuần" (avg 4 mặt + điểm cộng)
        public decimal? AverageScore { get; set; }

        public int? Ranking { get; set; }
    }

    public class UnitScoreSummaryData
    {
        private readonly SqlDb _db;

        public UnitScoreSummaryData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        /// <summary>
        /// Danh sách năm có dữ liệu tổng hợp (UnitScoreSummary).
        /// </summary>
        public List<int> GetAvailableYears()
        {
            var years = new List<int>();
            const string sql = @"SELECT DISTINCT [Year] FROM dbo.UnitScoreSummary ORDER BY [Year] DESC;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        if (!r.IsDBNull(0)) years.Add(r.GetInt32(0));
                    }
                }
            }

            return years;
        }

        /// <summary>
        /// Danh sách tháng có dữ liệu theo năm (UnitScoreSummary.Month IS NOT NULL).
        /// </summary>
        public List<int> GetAvailableMonths(int year)
        {
            var months = new List<int>();
            const string sql = @"
SELECT DISTINCT [Month]
FROM dbo.UnitScoreSummary
WHERE [Year] = @Year AND [Month] IS NOT NULL
ORDER BY [Month] ASC;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Year", year);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        if (!r.IsDBNull(0)) months.Add(r.GetInt32(0));
                    }
                }
            }

            return months;
        }

        /// <summary>
        /// Điểm TB tháng theo đơn vị: trung bình AverageScore của các tuần trong tháng.
        /// </summary>
        public List<UnitScoreAverageRow> GetMonthlyAverageScores(int year, int month)
        {
            var list = new List<UnitScoreAverageRow>();
            const string sql = @"
SELECT
    us.OrgUnitId,
    ISNULL(ou.Name, '') AS OrgUnitName,
    AVG(CAST(us.AverageScore AS decimal(18,4))) AS AvgScore
FROM dbo.UnitScoreSummary us
LEFT JOIN dbo.OrgUnits ou ON ou.Id = us.OrgUnitId
WHERE us.[Year] = @Year AND us.[Month] = @Month
GROUP BY us.OrgUnitId, ou.Name
ORDER BY AvgScore DESC, ou.Name ASC;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Year", year);
                cmd.Parameters.AddWithValue("@Month", month);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new UnitScoreAverageRow
                        {
                            OrgUnitId = SqlDb.GetInt(r, "OrgUnitId"),
                            OrgUnitName = SqlDb.GetString(r, "OrgUnitName"),
                            AverageScore = r["AvgScore"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["AvgScore"])
                        });
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Điểm TB năm theo đơn vị: trung bình các tháng (mỗi tháng tính TB theo tuần).
        /// </summary>
        public List<UnitScoreAverageRow> GetYearlyAverageScores(int year)
        {
            var list = new List<UnitScoreAverageRow>();
            const string sql = @"
WITH m AS (
    SELECT
        OrgUnitId,
        [Month],
        AVG(CAST(AverageScore AS decimal(18,4))) AS MonthAvg
    FROM dbo.UnitScoreSummary
    WHERE [Year] = @Year AND [Month] IS NOT NULL
    GROUP BY OrgUnitId, [Month]
)
SELECT
    m.OrgUnitId,
    ISNULL(ou.Name,'') AS OrgUnitName,
    AVG(m.MonthAvg) AS YearAvg
FROM m
LEFT JOIN dbo.OrgUnits ou ON ou.Id = m.OrgUnitId
GROUP BY m.OrgUnitId, ou.Name
ORDER BY YearAvg DESC, ou.Name ASC;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Year", year);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new UnitScoreAverageRow
                        {
                            OrgUnitId = SqlDb.GetInt(r, "OrgUnitId"),
                            OrgUnitName = SqlDb.GetString(r, "OrgUnitName"),
                            AverageScore = r["YearAvg"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["YearAvg"])
                        });
                    }
                }
            }

            return list;
        }

        // Giữ hàm cũ để tránh ảnh hưởng chỗ khác: mặc định Week=1
        public List<UnitScoreSummaryRow> GetByYearMonth(int year, int month) => GetByYearMonthWeek(year, month, 1);

        public List<UnitScoreSummaryRow> GetByYearMonthWeek(int year, int month, int week)
        {
            var list = new List<UnitScoreSummaryRow>();
            const string sql = @"
SELECT 
    us.OrgUnitId,
    ou.Name AS OrgUnitName,
    us.Week,
    us.ScoreArea1,
    us.ScoreArea2,
    us.ScoreArea3,
    us.ScoreArea4,
    us.AverageScore,
    us.Ranking
FROM dbo.UnitScoreSummary us
LEFT JOIN dbo.OrgUnits ou ON ou.Id = us.OrgUnitId
WHERE us.Year=@Year AND us.Month=@Month AND us.Week=@Week
ORDER BY ou.Name;";

            using (var conn = _db.CreateConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);
                    cmd.Parameters.AddWithValue("@Week", week);

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            list.Add(new UnitScoreSummaryRow
                            {
                                OrgUnitId = rd.GetInt32(0),
                                OrgUnitName = rd.IsDBNull(1) ? "" : rd.GetString(1),
                                Week = rd.IsDBNull(2) ? week : rd.GetInt32(2),
                                ScoreArea1 = rd.IsDBNull(3) ? (decimal?)null : rd.GetDecimal(3),
                                ScoreArea2 = rd.IsDBNull(4) ? (decimal?)null : rd.GetDecimal(4),
                                ScoreArea3 = rd.IsDBNull(5) ? (decimal?)null : rd.GetDecimal(5),
                                ScoreArea4 = rd.IsDBNull(6) ? (decimal?)null : rd.GetDecimal(6),
                                AverageScore = rd.IsDBNull(7) ? (decimal?)null : rd.GetDecimal(7),
                                Ranking = rd.IsDBNull(8) ? (int?)null : rd.GetInt32(8)
                            });
                        }
                    }
                }
            }

            return list;
        }

        // Giữ hàm cũ: mặc định Week=1
        public void Upsert(int year, int month, int orgUnitId,
            decimal? s1, decimal? s2, decimal? s3, decimal? s4,
            decimal? totalScore, int? ranking)
            => Upsert(year, month, 1, orgUnitId, s1, s2, s3, s4, totalScore, ranking);

        public void Upsert(int year, int month, int week, int orgUnitId,
            decimal? s1, decimal? s2, decimal? s3, decimal? s4,
            decimal? totalScore, int? ranking)
        {
            const string sql = @"
IF EXISTS(SELECT 1 FROM dbo.UnitScoreSummary WHERE Year=@Year AND Month=@Month AND Week=@Week AND OrgUnitId=@OrgUnitId)
BEGIN
    UPDATE dbo.UnitScoreSummary
    SET ScoreArea1=@S1,
        ScoreArea2=@S2,
        ScoreArea3=@S3,
        ScoreArea4=@S4,
        AverageScore=@Avg,
        Ranking=@Ranking
    WHERE Year=@Year AND Month=@Month AND Week=@Week AND OrgUnitId=@OrgUnitId;
END
ELSE
BEGIN
    INSERT INTO dbo.UnitScoreSummary(Year, Month, Week, OrgUnitId, ScoreArea1, ScoreArea2, ScoreArea3, ScoreArea4, AverageScore, Ranking)
    VALUES(@Year, @Month, @Week, @OrgUnitId, @S1, @S2, @S3, @S4, @Avg, @Ranking);
END";

            using (var conn = _db.CreateConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);
                    cmd.Parameters.AddWithValue("@Week", week);
                    cmd.Parameters.AddWithValue("@OrgUnitId", orgUnitId);

                    cmd.Parameters.AddWithValue("@S1", (object?)s1 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@S2", (object?)s2 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@S3", (object?)s3 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@S4", (object?)s4 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Avg", (object?)totalScore ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Ranking", (object?)ranking ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    public class UnitScoreAverageRow
    {
        public int OrgUnitId { get; set; }
        public string OrgUnitName { get; set; } = "";
        public decimal? AverageScore { get; set; }
    }
}