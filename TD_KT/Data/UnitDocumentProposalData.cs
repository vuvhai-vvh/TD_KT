using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using TD_KT.Models;
using TD_KT.Services;

namespace TD_KT.Data
{
    /// <summary>
    /// CRUD cho bảng dbo.UnitDocumentProposals.
    /// File mới được lưu trực tiếp vào cột FileData (VARBINARY(MAX)).
    /// FilePath được giữ để tương thích với các hồ sơ cũ.
    /// </summary>
    public class UnitDocumentProposalData
    {
        private readonly SqlDb _db;

        public UnitDocumentProposalData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider ?? new ConnectionStringProvider("Database"));
            EnsureFileDataColumn();
        }

        private void EnsureFileDataColumn()
        {
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
IF COL_LENGTH('dbo.UnitDocumentProposals', 'FileData') IS NULL
BEGIN
    ALTER TABLE dbo.UnitDocumentProposals ADD FileData VARBINARY(MAX) NULL;
END;";
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<int> GetYears()
        {
            var years = new List<int>();
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT DISTINCT [Year]
FROM dbo.UnitDocumentProposals
WHERE [Year] IS NOT NULL
ORDER BY [Year] DESC;";
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read()) if (!rd.IsDBNull(0)) years.Add(rd.GetInt32(0));
            }
            return years;
        }

        public List<UnitDocumentProposal> GetAll(int? year, int? month, int? orgUnitId, string search)
        {
            var list = new List<UnitDocumentProposal>();
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                var sql = @"
SELECT
    udp.Id, udp.Title, udp.OrgUnitId, ou.Name AS OrgUnitName,
    udp.FileName, udp.FileType, udp.FileSize, udp.FilePath,
    CASE WHEN udp.FileData IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS HasFileData,
    udp.UploadDate, udp.UploadedBy, udp.[Year], udp.[Month]
FROM dbo.UnitDocumentProposals udp
LEFT JOIN dbo.OrgUnits ou ON ou.Id = udp.OrgUnitId
WHERE 1=1
";
                if (year.HasValue)
                {
                    sql += " AND udp.[Year] = @Year";
                    cmd.Parameters.AddWithValue("@Year", year.Value);
                }
                if (month.HasValue)
                {
                    sql += " AND udp.[Month] = @Month";
                    cmd.Parameters.AddWithValue("@Month", month.Value);
                }
                if (orgUnitId.HasValue)
                {
                    sql += " AND udp.OrgUnitId = @OrgUnitId";
                    cmd.Parameters.AddWithValue("@OrgUnitId", orgUnitId.Value);
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    sql += " AND (udp.Title LIKE @Search OR udp.FileName LIKE @Search)";
                    cmd.Parameters.AddWithValue("@Search", "%" + search.Trim() + "%");
                }
                sql += " ORDER BY udp.UploadDate DESC, udp.Id DESC;";
                cmd.CommandText = sql;
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new UnitDocumentProposal
                        {
                            Id = SqlDb.GetInt(rd, "Id"),
                            Title = SqlDb.GetString(rd, "Title"),
                            OrgUnitId = SqlDb.GetNullableInt(rd, "OrgUnitId"),
                            OrgUnitName = SqlDb.GetString(rd, "OrgUnitName", ""),
                            FileName = SqlDb.GetString(rd, "FileName"),
                            FileType = SqlDb.GetString(rd, "FileType", ""),
                            FileSize = SqlDb.GetString(rd, "FileSize", ""),
                            FilePath = SqlDb.GetString(rd, "FilePath"),
                            HasFileData = SqlDb.GetBool(rd, "HasFileData"),
                            UploadDate = rd["UploadDate"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(rd["UploadDate"]),
                            UploadedBy = SqlDb.GetString(rd, "UploadedBy", ""),
                            Year = SqlDb.GetNullableInt(rd, "Year"),
                            Month = SqlDb.GetNullableInt(rd, "Month")
                        });
                    }
                }
            }
            return list;
        }

        public List<UnitDocumentProposal> GetLegacyFilesWithoutData()
        {
            var list = new List<UnitDocumentProposal>();
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT Id, FileName, FileType, FileSize, FilePath
FROM dbo.UnitDocumentProposals
WHERE FileData IS NULL AND ISNULL(LTRIM(RTRIM(FilePath)), N'') <> N'';";
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new UnitDocumentProposal
                        {
                            Id = SqlDb.GetInt(rd, "Id"),
                            FileName = SqlDb.GetString(rd, "FileName"),
                            FileType = SqlDb.GetString(rd, "FileType", ""),
                            FileSize = SqlDb.GetString(rd, "FileSize", ""),
                            FilePath = SqlDb.GetString(rd, "FilePath")
                        });
                    }
                }
            }
            return list;
        }

        public byte[] GetFileData(int id)
        {
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT FileData FROM dbo.UnitDocumentProposals WHERE Id = @Id;";
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                var value = cmd.ExecuteScalar();
                return value == null || value == DBNull.Value ? null : (byte[])value;
            }
        }

        public void StoreFileData(int id, byte[] fileData)
        {
            if (fileData == null || fileData.Length == 0) return;
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE dbo.UnitDocumentProposals SET FileData = @FileData WHERE Id = @Id AND FileData IS NULL;";
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.Add(new SqlParameter("@FileData", SqlDbType.VarBinary, -1) { Value = fileData });
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public int Insert(UnitDocumentProposal item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.UnitDocumentProposals
(
    Title, OrgUnitId, FileName, FileType, FileSize, FilePath, FileData,
    UploadDate, UploadedBy, [Year], [Month]
)
VALUES
(
    @Title, @OrgUnitId, @FileName, @FileType, @FileSize, @FilePath, @FileData,
    @UploadDate, @UploadedBy, @Year, @Month
);
SELECT SCOPE_IDENTITY();";
                AddCommonParameters(cmd, item, includeId: false);
                cmd.Parameters.Add(new SqlParameter("@FileData", SqlDbType.VarBinary, -1)
                {
                    Value = (object)item.FileData ?? DBNull.Value
                });
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void Update(UnitDocumentProposal item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var replaceFile = item.FileData != null;
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.UnitDocumentProposals
SET
    Title = @Title,
    OrgUnitId = @OrgUnitId,
    FileName = @FileName,
    FileType = @FileType,
    FileSize = @FileSize,
    FilePath = CASE WHEN @ReplaceFile = 1 THEN @FilePath ELSE FilePath END,
    FileData = CASE WHEN @ReplaceFile = 1 THEN @FileData ELSE FileData END,
    UploadedBy = @UploadedBy,
    [Year] = @Year,
    [Month] = @Month
WHERE Id = @Id;";
                AddCommonParameters(cmd, item, includeId: true);
                cmd.Parameters.AddWithValue("@ReplaceFile", replaceFile);
                cmd.Parameters.Add(new SqlParameter("@FileData", SqlDbType.VarBinary, -1)
                {
                    Value = replaceFile ? (object)item.FileData : DBNull.Value
                });
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static void AddCommonParameters(SqlCommand cmd, UnitDocumentProposal item, bool includeId)
        {
            if (includeId) cmd.Parameters.AddWithValue("@Id", item.Id);
            cmd.Parameters.AddWithValue("@Title", item.Title ?? "");
            cmd.Parameters.AddWithValue("@OrgUnitId", item.OrgUnitId.HasValue ? (object)item.OrgUnitId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@FileName", item.FileName ?? "");
            cmd.Parameters.AddWithValue("@FileType", (object)item.FileType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FileSize", (object)item.FileSize ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FilePath", item.FilePath ?? "");
            cmd.Parameters.AddWithValue("@UploadDate", item.UploadDate == default(DateTime) ? DateTime.Now : item.UploadDate);
            cmd.Parameters.AddWithValue("@UploadedBy", (object)item.UploadedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Year", item.Year.HasValue ? (object)item.Year.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Month", item.Month.HasValue ? (object)item.Month.Value : DBNull.Value);
        }

        public void Delete(int id)
        {
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM dbo.UnitDocumentProposals WHERE Id = @Id;";
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
