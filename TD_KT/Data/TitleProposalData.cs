using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using TD_KT.Services;

#nullable enable

namespace TD_KT.Data
{
    public class TitleProposalRow
    {
        public int Id { get; set; }
        public bool IsSelected { get; set; }
        public int No { get; set; }

        public int ProposalYear { get; set; }
        public int SoldierId { get; set; }

        public string FullName { get; set; } = "";
        public string Rank { get; set; } = "";
        public string Position { get; set; } = "";
        public string OrgUnit { get; set; } = "";

        public DateTime? EnlistmentDate { get; set; }
        public string EnlistmentDateText => EnlistmentDate?.ToString("dd/MM/yyyy") ?? "";

        public int YearsOfService { get; set; }
        public int MonthsOfService { get; set; }
        public string YearsOfServiceText => $"{YearsOfService} năm {MonthsOfService} tháng";

        public string ProposedTitle { get; set; } = "";
        public string Status { get; set; } = "";

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

    public class TitleProposalData
    {
        private readonly SqlDb _db;

        public TitleProposalData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<int> GetAvailableYears()
        {
            var years = new List<int>();
            const string sql = @"SELECT DISTINCT ProposalYear FROM dbo.TitleProposals ORDER BY ProposalYear DESC;";

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
            EnsureMultiTitleSupport();
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
                    var (years, months) = CalculateYearsMonths(s.EnlistmentDate, asOfDate);
                    var suggestedTitles = GetSuggestedRewards(years);
                    if (suggestedTitles.Count == 0)
                    {
                        const string deleteSql = "DELETE FROM dbo.TitleProposals WHERE ProposalYear=@Year AND SoldierId=@SoldierId;";
                        using (var del = new SqlCommand(deleteSql, conn))
                        {
                            del.Parameters.AddWithValue("@Year", year);
                            del.Parameters.AddWithValue("@SoldierId", s.SoldierId);
                            del.ExecuteNonQuery();
                        }
                        continue;
                    }

                    var deleteStaleSql = new SqlCommand(
                        $"DELETE FROM dbo.TitleProposals WHERE ProposalYear=@Year AND SoldierId=@SoldierId AND ProposedTitle NOT IN ({string.Join(", ", suggestedTitles.Select((_, i) => $"@Title{i}"))});",
                        conn);
                    deleteStaleSql.Parameters.AddWithValue("@Year", year);
                    deleteStaleSql.Parameters.AddWithValue("@SoldierId", s.SoldierId);
                    for (var i = 0; i < suggestedTitles.Count; i++)
                    {
                        deleteStaleSql.Parameters.AddWithValue($"@Title{i}", suggestedTitles[i]);
                    }
                    deleteStaleSql.ExecuteNonQuery();

                    const string upsert = @"
IF EXISTS (SELECT 1 FROM dbo.TitleProposals WHERE ProposalYear=@Year AND SoldierId=@SoldierId AND ProposedTitle=@ProposedTitle)
BEGIN
    UPDATE dbo.TitleProposals
    SET 
        YearsOfService=@YearsOfService,
        Status = CASE WHEN Status = N'Đã trao' THEN Status ELSE N'Chưa trao' END,
        ProposedTitle=@ProposedTitle,
        UpdatedAt=GETDATE()
    WHERE ProposalYear=@Year AND SoldierId=@SoldierId AND ProposedTitle=@ProposedTitle;
END
ELSE
BEGIN
    INSERT INTO dbo.TitleProposals(ProposalYear, SoldierId, YearsOfService, Status, ProposedTitle, Note, CreatedAt, UpdatedAt)
    VALUES(@Year, @SoldierId, @YearsOfService, N'Chưa trao', @ProposedTitle, N'', GETDATE(), NULL);
END";

                    foreach (var suggested in suggestedTitles)
                    {
                        using (var cmd = new SqlCommand(upsert, conn))
                        {
                            cmd.Parameters.AddWithValue("@Year", year);
                            cmd.Parameters.AddWithValue("@SoldierId", s.SoldierId);
                            cmd.Parameters.AddWithValue("@YearsOfService", years);
                            cmd.Parameters.AddWithValue("@ProposedTitle", suggested);
                            cmd.ExecuteNonQuery();
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        private void EnsureMultiTitleSupport()
        {
            const string sql = @"
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'TitleProposals_Year_Soldier')
BEGIN
    ALTER TABLE dbo.TitleProposals DROP CONSTRAINT [TitleProposals_Year_Soldier];
END

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UX_TitleProposals_Year_Soldier')
BEGIN
    ALTER TABLE dbo.TitleProposals DROP CONSTRAINT [UX_TitleProposals_Year_Soldier];
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'TitleProposals_Year_Soldier' AND object_id = OBJECT_ID(N'dbo.TitleProposals'))
BEGIN
    DROP INDEX [TitleProposals_Year_Soldier] ON dbo.TitleProposals;
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_TitleProposals_Year_Soldier' AND object_id = OBJECT_ID(N'dbo.TitleProposals'))
BEGIN
    DROP INDEX [UX_TitleProposals_Year_Soldier] ON dbo.TitleProposals;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'TitleProposals_Year_Soldier_Title' AND object_id = OBJECT_ID(N'dbo.TitleProposals'))
BEGIN
    CREATE UNIQUE INDEX [TitleProposals_Year_Soldier_Title] ON dbo.TitleProposals(ProposalYear, SoldierId, ProposedTitle);
END";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<TitleProposalRow> GetList(int? year, string? status, DateTime asOfDate)
        {
            var list = new List<TitleProposalRow>();

            const string sql = @"
SELECT 
    tp.Id,
    tp.ProposalYear,
    tp.SoldierId,
    COALESCE(s.FullName, '') AS FullName,
    COALESCE(NULLIF(r.Name, ''), '') AS Rank,
    COALESCE(NULLIF(p.Name, ''), '') AS Position,
    COALESCE(NULLIF(ou.Name, ''), '') AS OrgUnit,
    s.EnlistmentDate,
    tp.YearsOfService,
    ISNULL(tp.ProposedTitle,'') AS ProposedTitle,
    tp.Status,
    ISNULL(tp.Note, '') AS Note
FROM dbo.TitleProposals tp
LEFT JOIN dbo.Soldiers s ON s.Id = tp.SoldierId
LEFT JOIN dbo.Ranks r ON r.Id = s.RankId
LEFT JOIN dbo.Positions p ON p.Id = s.PositionId
LEFT JOIN dbo.OrgUnits ou ON ou.Id = s.OrgUnitId
WHERE
    (@Year IS NULL OR tp.ProposalYear = @Year)
    AND (@Status IS NULL OR tp.Status = @Status)
ORDER BY tp.Status ASC, tp.YearsOfService DESC, FullName ASC;";

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
                        var years = SqlDb.GetInt(r, "YearsOfService");
                        var months = enlist == null ? 0 : CalculateYearsMonths(enlist.Value, asOfDate).months;

                        list.Add(new TitleProposalRow
                        {
                            Id = SqlDb.GetInt(r, "Id"),
                            ProposalYear = SqlDb.GetInt(r, "ProposalYear"),
                            SoldierId = SqlDb.GetInt(r, "SoldierId"),
                            FullName = SqlDb.GetString(r, "FullName"),
                            Rank = SqlDb.GetString(r, "Rank"),
                            Position = SqlDb.GetString(r, "Position"),
                            OrgUnit = SqlDb.GetString(r, "OrgUnit"),
                            EnlistmentDate = enlist,
                            YearsOfService = years,
                            MonthsOfService = months,
                            ProposedTitle = SqlDb.GetString(r, "ProposedTitle"),
                            Status = SqlDb.GetString(r, "Status"),
                            Note = SqlDb.GetString(r, "Note"),
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

            using (var conn = _db.CreateConnection())
            {
                conn.Open();
                for (var i = 0; i < arr.Count; i++)
                {
                    using (var cmd = new SqlCommand("UPDATE dbo.TitleProposals SET Status = N'Đã trao', UpdatedAt = GETDATE() WHERE Id = @Id;", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", arr[i]);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static (int years, int months) CalculateYearsMonths(DateTime start, DateTime asOf)
        {
            if (asOf < start) return (0, 0);
            var totalMonths = (asOf.Year - start.Year) * 12 + (asOf.Month - start.Month);
            if (asOf.Day < start.Day) totalMonths--;
            if (totalMonths < 0) totalMonths = 0;
            var years = totalMonths / 12;
            var months = totalMonths % 12;
            return (years, months);
        }

        private static List<string> GetSuggestedRewards(int yearsOfService)
        {
            var results = new List<string>();
            if (yearsOfService >= 10) results.Add("Huy chương CSVV  hạng Ba");
            if (yearsOfService >= 15) results.Add("Huy chương CSVV  hạng Hai");
            if (yearsOfService >= 20) results.Add("Huy chương CSVV  hạng Nhất");
            if (yearsOfService >= 25) results.Add("Huy chương Quân kỳ Quyết thắng");
            return results;
        }
    }
}