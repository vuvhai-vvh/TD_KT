using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using TD_KT.Models;
using TD_KT.Services;

namespace TD_KT.Data
{
    /// <summary>
    /// CRUD cho bảng dbo.DocumentProposals.
    /// File mới được lưu trực tiếp vào cột FileData (VARBINARY(MAX)).
    /// FilePath được giữ để tương thích với các hồ sơ cũ.
    /// </summary>
    public class DocumentProposalData
    {
        private readonly SqlDb _db;

        public DocumentProposalData(IConnectionStringProvider csProvider)
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
IF COL_LENGTH('dbo.DocumentProposals', 'FileData') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentProposals ADD FileData VARBINARY(MAX) NULL;
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
FROM dbo.DocumentProposals
WHERE [Year] IS NOT NULL
ORDER BY [Year] DESC;";
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                        if (!rd.IsDBNull(0)) years.Add(rd.GetInt32(0));
                }
            }
            return years;
        }

        public List<string> GetDistinctProposingUnits()
        {
            var units = new List<string>();
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT DISTINCT ProposingUnit
FROM dbo.DocumentProposals
WHERE ProposingUnit IS NOT NULL AND LTRIM(RTRIM(ProposingUnit)) <> N''
ORDER BY ProposingUnit;";
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read()) units.Add(SqlDb.GetString(rd, "ProposingUnit"));
            }
            return units;
        }

        public List<DocumentProposal> GetAll(int? year, int? month, string proposingUnit, string search)
        {
            var list = new List<DocumentProposal>();
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                var sql = @"
SELECT
    Id, Title, ProposingUnit, FileName, FileType, FileSize, FilePath,
    CASE WHEN FileData IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS HasFileData,
    UploadDate, UploadedBy, [Year], [Month]
FROM dbo.DocumentProposals
WHERE 1=1
";
                if (year.HasValue)
                {
                    sql += " AND [Year] = @Year";
                    cmd.Parameters.AddWithValue("@Year", year.Value);
                }
                if (month.HasValue)
                {
                    sql += " AND [Month] = @Month";
                    cmd.Parameters.AddWithValue("@Month", month.Value);
                }
                if (!string.IsNullOrWhiteSpace(proposingUnit))
                {
                    sql += " AND ProposingUnit = @ProposingUnit";
                    cmd.Parameters.AddWithValue("@ProposingUnit", proposingUnit.Trim());
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    sql += " AND (Title LIKE @Search OR FileName LIKE @Search)";
                    cmd.Parameters.AddWithValue("@Search", "%" + search.Trim() + "%");
                }
                sql += " ORDER BY UploadDate DESC, Id DESC;";
                cmd.CommandText = sql;
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new DocumentProposal
                        {
                            Id = SqlDb.GetInt(rd, "Id"),
                            Title = SqlDb.GetString(rd, "Title"),
                            ProposingUnit = SqlDb.GetString(rd, "ProposingUnit", ""),
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

        public List<DocumentProposal> GetLegacyFilesWithoutData()
        {
            var list = new List<DocumentProposal>();
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT Id, FileName, FileType, FileSize, FilePath
FROM dbo.DocumentProposals
WHERE FileData IS NULL AND ISNULL(LTRIM(RTRIM(FilePath)), N'') <> N'';";
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new DocumentProposal
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
                cmd.CommandText = "SELECT FileData FROM dbo.DocumentProposals WHERE Id = @Id;";
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
                cmd.CommandText = "UPDATE dbo.DocumentProposals SET FileData = @FileData WHERE Id = @Id AND FileData IS NULL;";
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.Add(new SqlParameter("@FileData", SqlDbType.VarBinary, -1) { Value = fileData });
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public int Insert(DocumentProposal item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.DocumentProposals
(
    Title, ProposingUnit, FileName, FileType, FileSize, FilePath, FileData,
    UploadDate, UploadedBy, [Year], [Month]
)
VALUES
(
    @Title, @ProposingUnit, @FileName, @FileType, @FileSize, @FilePath, @FileData,
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

        public void Update(DocumentProposal item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var replaceFile = item.FileData != null;
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.DocumentProposals
SET
    Title = @Title,
    ProposingUnit = @ProposingUnit,
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

        private static void AddCommonParameters(SqlCommand cmd, DocumentProposal item, bool includeId)
        {
            if (includeId) cmd.Parameters.AddWithValue("@Id", item.Id);
            cmd.Parameters.AddWithValue("@Title", item.Title ?? "");
            cmd.Parameters.AddWithValue("@ProposingUnit", (object)item.ProposingUnit ?? DBNull.Value);
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
                cmd.CommandText = "DELETE FROM dbo.DocumentProposals WHERE Id = @Id;";
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
