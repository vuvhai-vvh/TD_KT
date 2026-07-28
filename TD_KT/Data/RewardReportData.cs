using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using TD_KT.Services;

namespace TD_KT.Data
{
    /// <summary>
    /// Data (ADO.NET) cho Tab Báo cáo khen thưởng (ReportStatisticView - Tab 2).
    /// </summary>
    public class RewardReportData
    {
        private readonly SqlDb _db;

        public RewardReportData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<int> GetAvailableReportYears()
        {
            var years = new List<int>();
            const string sql = @"
SELECT DISTINCT rrs.ReportYear AS [Year]
FROM dbo.RewardReportSnapshot rrs
WHERE rrs.ReportYear IS NOT NULL
ORDER BY [Year] DESC;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var y = SqlDb.GetInt(r, "Year");
                        if (y > 0) years.Add(y);
                    }
                }
            }
            return years;
        }

        public void SyncRewardReportSnapshotsFromDecisions()
        {
            const string sql = @"
SELECT
    dd.Id AS DecisionDetailId,
    d.Id AS DecisionId,
    CASE WHEN dd.SoldierId IS NOT NULL THEN N'Cá nhân' ELSE N'Tập thể' END AS RecipientType,
    dd.SoldierId,
    s.OrgUnitId,
    COALESCE(s.FullName, dd.FullName) AS RecipientName,
    dd.RewardContentId,
    dd.RewardFormId,
    d.IssuingLevelId AS IssuingLevelId,
    d.DecisionNumber,
    d.SignedDate,
    d.Signer,
    d.CreatedAt,
    YEAR(COALESCE(d.SignedDate, d.CreatedAt)) AS ReportYear
INTO #SrcSnapshot
FROM dbo.DecisionDetails dd
INNER JOIN dbo.Decisions d ON d.Id = dd.DecisionId
LEFT JOIN dbo.Soldiers s ON s.Id = dd.SoldierId;

UPDATE target
SET
    target.ReportYear = #SrcSnapshot.ReportYear,
    target.RecipientType = #SrcSnapshot.RecipientType,
    target.SoldierId = #SrcSnapshot.SoldierId,
    target.OrgUnitId = #SrcSnapshot.OrgUnitId,
    target.RecipientName = #SrcSnapshot.RecipientName,
    target.RewardContentId = #SrcSnapshot.RewardContentId,
    target.RewardFormId = #SrcSnapshot.RewardFormId,
    target.IssuingLevelId = #SrcSnapshot.IssuingLevelId,
    target.DecisionId = #SrcSnapshot.DecisionId,
    target.DecisionNumber = #SrcSnapshot.DecisionNumber,
    target.SignedDate = #SrcSnapshot.SignedDate,
    target.Signer = #SrcSnapshot.Signer,
    target.CreatedAt = #SrcSnapshot.CreatedAt
FROM dbo.RewardReportSnapshot target
INNER JOIN #SrcSnapshot ON target.DecisionDetailId = #SrcSnapshot.DecisionDetailId;

INSERT INTO dbo.RewardReportSnapshot
(
    ReportYear,
    RecipientType,
    SoldierId,
    OrgUnitId,
    RecipientName,
    RewardContentId,
    RewardFormId,
    IssuingLevelId,
    DecisionId,
    DecisionDetailId,
    DecisionNumber,
    SignedDate,
    Signer,
    CreatedAt
)
SELECT
    #SrcSnapshot.ReportYear,
    #SrcSnapshot.RecipientType,
    #SrcSnapshot.SoldierId,
    #SrcSnapshot.OrgUnitId,
    #SrcSnapshot.RecipientName,
    #SrcSnapshot.RewardContentId,
    #SrcSnapshot.RewardFormId,
    #SrcSnapshot.IssuingLevelId,
    #SrcSnapshot.DecisionId,
    #SrcSnapshot.DecisionDetailId,
    #SrcSnapshot.DecisionNumber,
    #SrcSnapshot.SignedDate,
    #SrcSnapshot.Signer,
    #SrcSnapshot.CreatedAt
FROM #SrcSnapshot
LEFT JOIN dbo.RewardReportSnapshot target ON target.DecisionDetailId = #SrcSnapshot.DecisionDetailId
WHERE target.DecisionDetailId IS NULL;

DROP TABLE #SrcSnapshot;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex) when (ex.Message.Contains("Invalid object name 'src'"))
                {
                    throw new InvalidOperationException(
                        "SQL báo lỗi 'Invalid object name ''src'''. Đây thường xảy ra khi chạy bản SQL cũ có alias 'src'. " +
                        "Vui lòng chắc chắn đã build/deploy đúng phiên bản mới hoặc kiểm tra trigger/procedure trong DB.",
                        ex);
                }
            }
        }


        public List<int> GetAvailableRewardHistoryYears()
        {
            var years = new List<int>();
            const string sql = @"
SELECT DISTINCT rh.RewardYear AS [Year]
FROM dbo.RewardHistory rh
WHERE rh.RewardYear IS NOT NULL
ORDER BY [Year] DESC;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var y = SqlDb.GetInt(r, "Year");
                        if (y > 0) years.Add(y);
                    }
                }
            }
            return years;
        }

        public List<RewardFormOption> GetRewardForms()
        {
            var result = new List<RewardFormOption>();
            const string sql = "SELECT Id, Name FROM dbo.RewardForms ORDER BY Name;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new RewardFormOption
                        {
                            Id = SqlDb.GetInt(r, "Id"),
                            Name = SqlDb.GetString(r, "Name")
                        });
                    }
                }
            }
            return result;
        }
        public List<IssuingLevelOption> GetIssuingLevels()
        {
            var result = new List<IssuingLevelOption>();
            const string sql = "SELECT Id, Name FROM dbo.IssuingLevels WHERE IsActive = 1 ORDER BY Name;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new IssuingLevelOption
                        {
                            Id = SqlDb.GetInt(r, "Id"),
                            Name = SqlDb.GetString(r, "Name")
                        });
                    }
                }
            }
            return result;
        }

        public void SyncRewardHistoryFromReportSnapshots()
        {
            const string sql = @"
INSERT INTO dbo.RewardHistory
(
    SoldierId,
    FullName,
    Rank,
    PositionUnit,
    RewardYear,
    RecipientType,
    OrgUnitId,
    RecipientName,
    RewardContentId,
    RewardFormId,
    IssuingLevelId,
    DecisionNumber,
    DecisionId,
    DecisionDetailId,
    SignedDate,
    Signer,
    CreatedAt
)
SELECT
    rrs.SoldierId,
    ISNULL(rrs.RecipientName, '') AS FullName,
    dd.Rank,
    dd.PositionUnit,
    rrs.ReportYear AS RewardYear,
    rrs.RecipientType,
    rrs.OrgUnitId,
    rrs.RecipientName,
    rrs.RewardContentId,
    rrs.RewardFormId,
    rrs.IssuingLevelId AS IssuingLevelId,
    rrs.DecisionNumber,
    rrs.DecisionId,
    rrs.DecisionDetailId,
    rrs.SignedDate,
    rrs.Signer,
    rrs.CreatedAt
FROM dbo.RewardReportSnapshot rrs
INNER JOIN dbo.DecisionDetails dd ON dd.Id = rrs.DecisionDetailId
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.RewardHistory rh
    WHERE rh.DecisionDetailId = rrs.DecisionDetailId
      AND rh.RecipientName = rrs.RecipientName
      AND rh.RewardYear = rrs.ReportYear
      AND ISNULL(rh.RewardContentId, 0) = ISNULL(rrs.RewardContentId, 0)
      AND ISNULL(rh.RewardFormId, 0) = ISNULL(rrs.RewardFormId, 0)
);";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex) when (ex.Message.Contains("Invalid object name 'src'"))
                {
                    throw new InvalidOperationException(
                        "SQL báo lỗi 'Invalid object name ''src'''. Đây thường xảy ra khi chạy bản SQL cũ có alias 'src'. " +
                        "Vui lòng chắc chắn đã build/deploy đúng phiên bản mới hoặc kiểm tra trigger/procedure trong DB.",
                        ex);
                }
            }
        }

        public List<RewardReportRowDto> GetRewardReportRows(int? year, int? issuingLevelId, int? rewardFormId, string search)
        {
            var rows = new List<RewardReportRowDto>();

            const string sql = @"
SELECT
    rrs.RecipientName AS SubjectName,
    ISNULL(rf.Name, '') AS RewardContentName,
    ISNULL(rf.Name, '') AS RewardFormName,
    ISNULL(il.Name, '') AS IssuingLevelName,
    ISNULL(ou.Name, '') AS OrgUnitName,
    ISNULL(rrs.DecisionNumber, '') AS DecisionNumber,
    rrs.SignedDate AS SignedOrCreatedDate,
    ISNULL(rrs.Signer, '') AS Signer,
    rrs.ReportYear AS [Year],
    rrs.Id AS OrderNo
FROM dbo.RewardReportSnapshot rrs
LEFT JOIN dbo.RewardForms rf ON rf.Id = rrs.RewardFormId
LEFT JOIN dbo.IssuingLevels il ON il.Id = rrs.IssuingLevelId
LEFT JOIN dbo.OrgUnits ou ON ou.Id = rrs.OrgUnitId
WHERE
    (@Year IS NULL OR rrs.ReportYear = @Year)
    AND (@IssuingLevelId IS NULL OR rrs.IssuingLevelId = @IssuingLevelId)
    AND (@RewardFormId IS NULL OR rrs.RewardFormId = @RewardFormId)
    AND (
        @Search IS NULL OR
        rrs.RecipientName LIKE @SearchLike OR
        ISNULL(rf.Name, '') LIKE @SearchLike OR
        ISNULL(rrs.DecisionNumber, '') LIKE @SearchLike OR
        ISNULL(il.Name, '') LIKE @SearchLike OR
        ISNULL(rrs.Signer, '') LIKE @SearchLike
    )
ORDER BY rrs.ReportYear DESC, rrs.Id DESC; ";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Year", SqlDbType.Int).Value = (object)year ?? DBNull.Value;
                cmd.Parameters.Add("@IssuingLevelId", SqlDbType.Int).Value = (object)issuingLevelId ?? DBNull.Value;
                cmd.Parameters.Add("@RewardFormId", SqlDbType.Int).Value = (object)rewardFormId ?? DBNull.Value;

                if (string.IsNullOrWhiteSpace(search))
                {
                    cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 200).Value = DBNull.Value;
                    cmd.Parameters.Add("@SearchLike", SqlDbType.NVarChar, 220).Value = DBNull.Value;
                }
                else
                {
                    var s = search.Trim();
                    cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 200).Value = s;
                    cmd.Parameters.Add("@SearchLike", SqlDbType.NVarChar, 220).Value =
                        "%" + s.Replace("%", "[%]").Replace("_", "[_]") + "%";
                }

                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        rows.Add(new RewardReportRowDto
                        {
                            SubjectName = SqlDb.GetString(r, "SubjectName"),
                            RewardContentName = SqlDb.GetString(r, "RewardContentName"),
                            RewardFormName = SqlDb.GetString(r, "RewardFormName"),
                            IssuingLevelName = SqlDb.GetString(r, "IssuingLevelName"),
                            OrgUnitName = SqlDb.GetString(r, "OrgUnitName"),
                            DecisionNumber = SqlDb.GetString(r, "DecisionNumber"),
                            SignedOrCreatedDate = r.IsDBNull(r.GetOrdinal("SignedOrCreatedDate"))
                                ? (DateTime?)null
                                : r.GetDateTime(r.GetOrdinal("SignedOrCreatedDate")),
                            Signer = SqlDb.GetString(r, "Signer"),
                            OrderNo = SqlDb.GetInt(r, "OrderNo")
                        });
                    }
                }
            }

            return rows;
        }

        /// <summary>
        /// Dữ liệu thô cho Tab 3: Thống kê khen thưởng qua các năm.
        /// Lưu ý: Nếu endYear != null, chỉ lấy các dòng có năm <= endYear để phục vụ thống kê "liên tiếp" kết thúc ở endYear.
        /// </summary>
        public List<RewardYearRawRowDto> GetRewardYearRawRowsFromSnapshots(int? endYear, string search)
        {
            var rows = new List<RewardYearRawRowDto>();

            const string sql = @"
    SELECT
    rrs.RecipientName AS SubjectName,
    ISNULL(rf.Name, '') AS RewardContentName,
    ISNULL(rf.Name, '') AS RewardFormName,
    ISNULL(il.Name, '') AS IssuingLevelName,
    ISNULL(ou.Name, '') AS OrgUnitName,
    ISNULL(rrs.DecisionNumber, '') AS DecisionNumber,
    COALESCE(rrs.SignedDate, rrs.CreatedAt) AS SignedOrCreatedDate,
    rrs.ReportYear AS [Year],
    rrs.Id AS OrderNo
FROM dbo.RewardReportSnapshot rrs
LEFT JOIN dbo.RewardForms rf ON rf.Id = rrs.RewardFormId
LEFT JOIN dbo.IssuingLevels il ON il.Id = rrs.IssuingLevelId
LEFT JOIN dbo.OrgUnits ou ON ou.Id = rrs.OrgUnitId
	WHERE
	    (@EndYear IS NULL OR rrs.ReportYear <= @EndYear)
	    AND (
	        @SearchLike IS NULL OR
	        rrs.RecipientName LIKE @SearchLike OR
            ISNULL(rf.Name, '') LIKE @SearchLike OR
	        ISNULL(rrs.DecisionNumber, '') LIKE @SearchLike OR
        ISNULL(il.Name, '') LIKE @SearchLike
	        )
	ORDER BY rrs.ReportYear DESC, rrs.Id DESC; ";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@EndYear", SqlDbType.Int).Value = (object)endYear ?? DBNull.Value;

                cmd.Parameters.Add("@SearchLike", SqlDbType.NVarChar, 220).Value = string.IsNullOrWhiteSpace(search)
                    ? (object)DBNull.Value
                    : $"%{search.Trim().Replace("%", "[%]").Replace("_", "[_]")}%";

                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        rows.Add(new RewardYearRawRowDto
                        {
                            SubjectName = SqlDb.GetString(r, "SubjectName"),
                            RewardContentName = SqlDb.GetString(r, "RewardContentName"),
                            RewardFormName = SqlDb.GetString(r, "RewardFormName"),
                            IssuingLevelName = SqlDb.GetString(r, "IssuingLevelName"),
                            OrgUnitName = SqlDb.GetString(r, "OrgUnitName"),
                            DecisionNumber = SqlDb.GetString(r, "DecisionNumber"),
                            SignedOrCreatedDate = r.IsDBNull(r.GetOrdinal("SignedOrCreatedDate"))
                                ? (DateTime?)null
                                : r.GetDateTime(r.GetOrdinal("SignedOrCreatedDate")),
                            Year = SqlDb.GetInt(r, "Year"),
                            OrderNo = SqlDb.GetInt(r, "OrderNo")
                        });
                    }
                }
            }

            return rows;
        }
    }

    public class RewardFormOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class RewardReportRowDto
    {
        public string SubjectName { get; set; }
        public string RewardContentName { get; set; }
        public string RewardFormName { get; set; }
        public string IssuingLevelName { get; set; }
        public string OrgUnitName { get; set; }
        public string DecisionNumber { get; set; }
        public DateTime? SignedOrCreatedDate { get; set; }
        public string Signer { get; set; }
        public int OrderNo { get; set; }
    }

    public class IssuingLevelOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class RewardYearRawRowDto
    {
        public string SubjectName { get; set; }
        public string RewardContentName { get; set; }
        public string RewardFormName { get; set; }
        public string IssuingLevelName { get; set; }

        public string OrgUnitName { get; set; }
        public string DecisionNumber { get; set; }
        public DateTime? SignedOrCreatedDate { get; set; }
        public int Year { get; set; }
        public int OrderNo { get; set; }
    }
}
