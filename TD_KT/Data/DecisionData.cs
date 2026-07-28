using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using TD_KT.ViewModels;

namespace TD_KT.Data
{
    public class DecisionHeaderRow
    {
        public int Id { get; set; }
        public string DecisionNumber { get; set; } = string.Empty;
        public string DecisionContent { get; set; } = string.Empty;
        public int IssuingLevelId { get; set; }
        public string IssuingLevelName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public string Note { get; set; } = string.Empty;

        // Added by patch
        public string Signer { get; set; } = string.Empty;
        public DateTime? SignedDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public string DisplayText => $"{DecisionNumber} (ID:{Id})";
    }

    public class DecisionData
    {
        private readonly string _connectionString;

        public DecisionData(string connectionStringName = "Database")
        {
            var cs = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
                throw new InvalidOperationException($"No connection string named '{connectionStringName}' could be found in the application config file.");
            _connectionString = cs.ConnectionString;
        }

        public List<DecisionHeaderRow> GetAll()
        {
            var list = new List<DecisionHeaderRow>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT d.Id,
       d.DecisionNumber,
       d.DecisionContent,
       d.IssuingLevelId,
       il.Name AS IssuingLevelName,
       d.TotalRows,
       d.Note,
       d.Signer,
       d.SignedDate,
       d.CreatedAt
FROM dbo.Decisions d
LEFT JOIN dbo.IssuingLevels il ON il.Id = d.IssuingLevelId
ORDER BY d.CreatedAt DESC, d.Id DESC;";
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                        list.Add(ReadHeader(rd));
                }
            }
            return list;
        }

        public List<int> GetDistinctYears()
        {
            var years = new HashSet<int>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT DISTINCT YEAR(CreatedAt) AS Y
FROM dbo.Decisions
WHERE CreatedAt IS NOT NULL
ORDER BY YEAR(CreatedAt) DESC;";
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        if (rd["Y"] != DBNull.Value)
                            years.Add(Convert.ToInt32(rd["Y"]));
                    }
                }
            }

            // Nếu DB chưa có dữ liệu: trả về năm hiện tại để combo không rỗng
            if (years.Count == 0)
                years.Add(DateTime.Now.Year);

            years.Add(2023);
            years.Add(2024);
            years.Add(2025);
            years.Add(DateTime.Now.Year);

            return years
                .Where(y => y > 0)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();
        }

        public List<DecisionHeaderRow> GetAllFiltered(int? year, int? month, string keyword)
        {
            var list = new List<DecisionHeaderRow>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT d.Id,
       d.DecisionNumber,
       d.DecisionContent,
       d.IssuingLevelId,
       il.Name AS IssuingLevelName,
       d.TotalRows,
       d.Note,
       d.Signer,
       d.SignedDate,
       d.CreatedAt
FROM dbo.Decisions d
LEFT JOIN dbo.IssuingLevels il ON il.Id = d.IssuingLevelId
WHERE (@Year IS NULL OR YEAR(d.CreatedAt) = @Year)
  AND (@Month IS NULL OR MONTH(d.CreatedAt) = @Month)
  AND (@Keyword IS NULL OR DecisionNumber LIKE '%' + @Keyword + '%' OR DecisionContent LIKE '%' + @Keyword + '%')
ORDER BY d.CreatedAt DESC, d.Id DESC;";

                cmd.Parameters.AddWithValue("@Year", (object)year ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Month", (object)month ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Keyword", (object)keyword ?? DBNull.Value);

                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                        list.Add(ReadHeader(rd));
                }
            }
            return list;
        }

        public DecisionHeaderRow GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT d.Id,
       d.DecisionNumber,
       d.DecisionContent,
       d.IssuingLevelId,
       il.Name AS IssuingLevelName,
       d.TotalRows,
       d.Note,
       d.Signer,
       d.SignedDate,
       d.CreatedAt
FROM dbo.Decisions d
LEFT JOIN dbo.IssuingLevels il ON il.Id = d.IssuingLevelId
WHERE Id=@Id;";
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;
                    return ReadHeader(rd);
                }
            }
        }

        public int Insert(DecisionHeaderRow row)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.Decisions(DecisionNumber, DecisionContent, IssuingLevelId, TotalRows, Note, Signer, SignedDate, CreatedAt)
VALUES(@DecisionNumber, @DecisionContent, @IssuingLevelId, @TotalRows, @Note, @Signer, @SignedDate, GETDATE());
SELECT SCOPE_IDENTITY();";

                cmd.Parameters.AddWithValue("@DecisionNumber", row.DecisionNumber);
                cmd.Parameters.AddWithValue("@DecisionContent", row.DecisionContent);
                cmd.Parameters.AddWithValue("@IssuingLevelId", row.IssuingLevelId);
                cmd.Parameters.AddWithValue("@TotalRows", row.TotalRows);
                cmd.Parameters.AddWithValue("@Note", (object)row.Note ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Signer", (object)row.Signer ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SignedDate", (object)row.SignedDate ?? DBNull.Value);

                try
                {
                    conn.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch (SqlException ex) when (ex.Message.Contains("Invalid object name 'src'"))
                {
                    throw new InvalidOperationException(
                        "SQL báo lỗi 'Invalid object name ''src'''. Điều này thường do trigger/procedure trong DB " +
                        "đang dùng alias 'src' hoặc ứng dụng đang chạy bản SQL cũ. " +
                        "Vui lòng kiểm tra trigger trên bảng DecisionDetails hoặc cập nhật lại phiên bản ứng dụng/DB.",
                        ex);
                }
            }
        }

        public void Update(DecisionHeaderRow row)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.Decisions
SET DecisionNumber=@DecisionNumber,
    DecisionContent=@DecisionContent,
    IssuingLevelId=@IssuingLevelId,
    TotalRows=@TotalRows,
    Note=@Note,
    Signer=@Signer,
    SignedDate=@SignedDate
WHERE Id=@Id;";

                cmd.Parameters.AddWithValue("@Id", row.Id);
                cmd.Parameters.AddWithValue("@DecisionNumber", row.DecisionNumber);
                cmd.Parameters.AddWithValue("@DecisionContent", row.DecisionContent);
                cmd.Parameters.AddWithValue("@IssuingLevelId", row.IssuingLevelId);
                cmd.Parameters.AddWithValue("@TotalRows", row.TotalRows);
                cmd.Parameters.AddWithValue("@Note", (object)row.Note ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Signer", (object)row.Signer ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SignedDate", (object)row.SignedDate ?? DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM dbo.Decisions WHERE Id=@Id;";
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<DecisionDetailDisplay> GetDetails(int decisionId)
        {
            var list = new List<DecisionDetailDisplay>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                // JOIN để đồng bộ theo bảng danh mục/quân nhân:
                // - SoldierId => lấy lại FullName/Rank/Position/OrgUnit mới nhất
                // - RewardFormId => lấy tên hình thức khen thưởng
                // Nếu dòng dữ liệu cũ chưa có các Id, sẽ fallback về các cột text đã lưu.
                cmd.CommandText = @"
SELECT
    dd.Id,
    dd.DecisionId,
    dd.OrderNo,
    dd.SoldierId,
    COALESCE(s.FullName, dd.FullName) AS FullName,
    COALESCE(r.Name, dd.Rank) AS Rank,
    COALESCE(
        NULLIF(LTRIM(RTRIM(
            CONCAT(
                p.Name,
                CASE WHEN p.Name IS NOT NULL AND ou.Name IS NOT NULL THEN N' - ' ELSE N'' END,
                ou.Name
            )
        )), N''),
        dd.PositionUnit
    ) AS PositionUnit,
    COALESCE(dd.BirthYear, s.BirthYear) AS BirthYear,
    COALESCE(dd.EnlistmentDate, CASE WHEN s.EnlistmentDate IS NULL THEN NULL ELSE FORMAT(s.EnlistmentDate, 'dd/MM/yyyy') END) AS EnlistmentDate,
    COALESCE(dd.Hometown, s.Hometown) AS Hometown,
    dd.RewardContentId,
    COALESCE(rf.Name, '') AS RewardContentName,
    dd.RewardFormId,
    COALESCE(rf.Name, '') AS RewardFormName,
    dd.Circumstance,
    dd.Note
FROM dbo.DecisionDetails dd
LEFT JOIN dbo.Soldiers s ON dd.SoldierId = s.Id
LEFT JOIN dbo.Ranks r ON s.RankId = r.Id
LEFT JOIN dbo.Positions p ON s.PositionId = p.Id
LEFT JOIN dbo.OrgUnits ou ON s.OrgUnitId = ou.Id
LEFT JOIN dbo.RewardForms rf ON dd.RewardFormId = rf.Id
WHERE dd.DecisionId=@DecisionId
ORDER BY dd.OrderNo, dd.Id;";

                cmd.Parameters.AddWithValue("@DecisionId", decisionId);
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new DecisionDetailDisplay
                        {
                            Id = Convert.ToInt32(rd["Id"]),
                            DecisionId = Convert.ToInt32(rd["DecisionId"]),
                            STT = Convert.ToInt32(rd["OrderNo"]),
                            SoldierId = rd["SoldierId"] == DBNull.Value ? (int?)null : Convert.ToInt32(rd["SoldierId"]),
                            HoTen = rd["FullName"] as string ?? "",
                            CapBac = rd["Rank"] as string ?? "",
                            ChucVuDonVi = rd["PositionUnit"] as string ?? "",
                            NamSinh = rd["BirthYear"] == DBNull.Value ? (int?)null : Convert.ToInt32(rd["BirthYear"]),
                            NhapNgu = rd["EnlistmentDate"] as string ?? "",
                            QueQuan = rd["Hometown"] as string ?? "",
                            RewardContentId = rd["RewardContentId"] == DBNull.Value ? (int?)null : Convert.ToInt32(rd["RewardContentId"]),
                            NoiDungKT = rd["RewardContentName"] as string ?? "",
                            RewardFormId = rd["RewardFormId"] == DBNull.Value ? (int?)null : Convert.ToInt32(rd["RewardFormId"]),
                            HinhThucKT = rd["RewardFormName"] as string ?? "",
                            HoanCanh = rd["Circumstance"] as string ?? "",
                            GhiChu = rd["Note"] as string ?? "",
                        });
                    }
                }
            }
            return list;
        }

        public int InsertDetail(int decisionId, DecisionDetailDisplay d)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.DecisionDetails(
    DecisionId, OrderNo,
    SoldierId,
    FullName, Rank, PositionUnit, BirthYear, EnlistmentDate, Hometown,
    RewardContentId,
    RewardFormId,
    Circumstance, Note
)
VALUES(
    @DecisionId, @OrderNo,
    @SoldierId,
    @FullName, @Rank, @PositionUnit, @BirthYear, @EnlistmentDate, @Hometown,
    @RewardContentId,
    @RewardFormId,
    @Circumstance, @Note
);
SELECT SCOPE_IDENTITY();";

                cmd.Parameters.AddWithValue("@DecisionId", decisionId);
                cmd.Parameters.AddWithValue("@OrderNo", d.STT);
                cmd.Parameters.AddWithValue("@SoldierId", (object)d.SoldierId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FullName", d.HoTen);
                cmd.Parameters.AddWithValue("@Rank", (object)d.CapBac ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PositionUnit", (object)d.ChucVuDonVi ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BirthYear", (object)d.NamSinh ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EnlistmentDate", (object)d.NhapNgu ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Hometown", (object)d.QueQuan ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RewardContentId", (object)d.RewardContentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RewardFormId", (object)d.RewardFormId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Circumstance", (object)d.HoanCanh ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Note", (object)d.GhiChu ?? DBNull.Value);

                try
                {
                    conn.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch (SqlException ex) when (ex.Message.Contains("Invalid object name 'src'"))
                {
                    throw new InvalidOperationException(
                        "SQL báo lỗi 'Invalid object name ''src'''. Điều này thường do trigger/procedure trong DB " +
                        "đang dùng alias 'src' hoặc ứng dụng đang chạy bản SQL cũ. " +
                        "Vui lòng kiểm tra trigger trên bảng DecisionDetails hoặc cập nhật lại phiên bản ứng dụng/DB.",
                        ex);
                }
            }
        }

        public void UpdateDetail(DecisionDetailDisplay d)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.DecisionDetails
SET OrderNo=@OrderNo,
    SoldierId=@SoldierId,
    FullName=@FullName,
    Rank=@Rank,
    PositionUnit=@PositionUnit,
    BirthYear=@BirthYear,
    EnlistmentDate=@EnlistmentDate,
    Hometown=@Hometown,
    RewardContentId=@RewardContentId,
    RewardFormId=@RewardFormId,
    Circumstance=@Circumstance,
    Note=@Note
WHERE Id=@Id;";

                cmd.Parameters.AddWithValue("@Id", d.Id);
                cmd.Parameters.AddWithValue("@OrderNo", d.STT);
                cmd.Parameters.AddWithValue("@SoldierId", (object)d.SoldierId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FullName", d.HoTen);
                cmd.Parameters.AddWithValue("@Rank", (object)d.CapBac ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PositionUnit", (object)d.ChucVuDonVi ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BirthYear", (object)d.NamSinh ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EnlistmentDate", (object)d.NhapNgu ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Hometown", (object)d.QueQuan ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RewardContentId", (object)d.RewardContentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RewardFormId", (object)d.RewardFormId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Circumstance", (object)d.HoanCanh ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Note", (object)d.GhiChu ?? DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteDetail(int detailId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM dbo.DecisionDetails WHERE Id=@Id;";
                cmd.Parameters.AddWithValue("@Id", detailId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateTotalRows(int decisionId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.Decisions
SET TotalRows = (SELECT COUNT(*) FROM dbo.DecisionDetails WHERE DecisionId=@DecisionId)
WHERE Id=@DecisionId;";
                cmd.Parameters.AddWithValue("@DecisionId", decisionId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static DecisionHeaderRow ReadHeader(IDataRecord rd)
        {
            return new DecisionHeaderRow
            {
                Id = Convert.ToInt32(rd["Id"]),
                DecisionNumber = rd["DecisionNumber"] as string ?? "",
                DecisionContent = rd["DecisionContent"] as string ?? "",
                IssuingLevelId = rd["IssuingLevelId"] == DBNull.Value ? 0 : Convert.ToInt32(rd["IssuingLevelId"]),
                IssuingLevelName = rd["IssuingLevelName"] as string ?? "",
                TotalRows = rd["TotalRows"] == DBNull.Value ? 0 : Convert.ToInt32(rd["TotalRows"]),
                Note = rd["Note"] as string ?? "",
                Signer = rd["Signer"] as string ?? "",
                SignedDate = rd["SignedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["SignedDate"]),
                CreatedAt = rd["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(rd["CreatedAt"])
            };
        }
    }
}