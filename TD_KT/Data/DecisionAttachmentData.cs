using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using TD_KT.ViewModels;

namespace TD_KT.Data
{
    /// <summary>
    /// CRUD cho bảng dbo.DecisionAttachments.
    /// File mới được lưu trực tiếp vào cột FileData (VARBINARY(MAX)).
    /// FilePath được giữ để tương thích với các file cũ.
    /// </summary>
    public class DecisionAttachmentData
    {
        private readonly string _connectionString;

        public DecisionAttachmentData(string connectionStringName = "Database")
        {
            var cs = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
                throw new InvalidOperationException($"No connection string named '{connectionStringName}' could be found in the application config file.");

            _connectionString = cs.ConnectionString;
            EnsureRequiredColumns();
        }

        private void EnsureRequiredColumns()
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
IF COL_LENGTH('dbo.DecisionAttachments', 'DecisionDetailId') IS NULL
BEGIN
    ALTER TABLE dbo.DecisionAttachments ADD DecisionDetailId INT NULL;
END;

IF COL_LENGTH('dbo.DecisionAttachments', 'FileData') IS NULL
BEGIN
    ALTER TABLE dbo.DecisionAttachments ADD FileData VARBINARY(MAX) NULL;
END;";
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<DecisionAttachmentDisplay> GetByDecision(int decisionId, int? decisionDetailId = null)
        {
            var list = new List<DecisionAttachmentDisplay>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT
    Id, DecisionId, DecisionDetailId, FileName, FileType, FileSize, FilePath,
    CASE WHEN FileData IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS HasFileData,
    UploadDate, UploadedBy
FROM dbo.DecisionAttachments
WHERE DecisionId = @DecisionId
  AND ((@DecisionDetailId IS NULL AND DecisionDetailId IS NULL) OR DecisionDetailId = @DecisionDetailId)
ORDER BY UploadDate DESC, Id DESC;";
                cmd.Parameters.Add("@DecisionId", SqlDbType.Int).Value = decisionId;
                cmd.Parameters.Add("@DecisionDetailId", SqlDbType.Int).Value =
                    decisionDetailId.HasValue ? (object)decisionDetailId.Value : DBNull.Value;
                conn.Open();

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new DecisionAttachmentDisplay
                        {
                            Id = Convert.ToInt32(rd["Id"]),
                            DecisionId = Convert.ToInt32(rd["DecisionId"]),
                            DecisionDetailId = rd["DecisionDetailId"] == DBNull.Value
                                ? (int?)null
                                : Convert.ToInt32(rd["DecisionDetailId"]),
                            FileName = rd["FileName"] as string ?? string.Empty,
                            FileType = rd["FileType"] as string ?? string.Empty,
                            FileSize = rd["FileSize"] == DBNull.Value
                                ? (long?)null
                                : Convert.ToInt64(rd["FileSize"]),
                            FilePath = rd["FilePath"] as string ?? string.Empty,
                            HasFileData = rd["HasFileData"] != DBNull.Value && Convert.ToBoolean(rd["HasFileData"]),
                            UploadDate = rd["UploadDate"] == DBNull.Value
                                ? DateTime.MinValue
                                : Convert.ToDateTime(rd["UploadDate"]),
                            UploadedBy = rd["UploadedBy"] as string ?? string.Empty
                        });
                    }
                }
            }

            return list;
        }

        public List<DecisionAttachmentDisplay> GetLegacyFilesWithoutData()
        {
            var list = new List<DecisionAttachmentDisplay>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT Id, DecisionId, DecisionDetailId, FileName, FileType, FileSize, FilePath
FROM dbo.DecisionAttachments
WHERE FileData IS NULL
  AND ISNULL(LTRIM(RTRIM(FilePath)), N'') <> N'';";
                conn.Open();

                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new DecisionAttachmentDisplay
                        {
                            Id = Convert.ToInt32(rd["Id"]),
                            DecisionId = Convert.ToInt32(rd["DecisionId"]),
                            DecisionDetailId = rd["DecisionDetailId"] == DBNull.Value
                                ? (int?)null
                                : Convert.ToInt32(rd["DecisionDetailId"]),
                            FileName = rd["FileName"] as string ?? string.Empty,
                            FileType = rd["FileType"] as string ?? string.Empty,
                            FileSize = rd["FileSize"] == DBNull.Value
                                ? (long?)null
                                : Convert.ToInt64(rd["FileSize"]),
                            FilePath = rd["FilePath"] as string ?? string.Empty
                        });
                    }
                }
            }

            return list;
        }

        public byte[] GetFileData(int attachmentId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT FileData
FROM dbo.DecisionAttachments
WHERE Id = @Id;";
                cmd.Parameters.AddWithValue("@Id", attachmentId);
                conn.Open();

                var value = cmd.ExecuteScalar();
                return value == null || value == DBNull.Value ? null : (byte[])value;
            }
        }

        public void StoreFileData(int attachmentId, byte[] fileData)
        {
            if (fileData == null || fileData.Length == 0)
                return;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.DecisionAttachments
SET FileData = @FileData
WHERE Id = @Id
  AND FileData IS NULL;";
                cmd.Parameters.AddWithValue("@Id", attachmentId);
                cmd.Parameters.Add(new SqlParameter("@FileData", SqlDbType.VarBinary, -1)
                {
                    Value = fileData
                });
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public int Insert(DecisionAttachmentDisplay attachment)
        {
            if (attachment == null)
                throw new ArgumentNullException(nameof(attachment));

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.DecisionAttachments
(
    DecisionId, DecisionDetailId, FileName, FileType, FileSize,
    FilePath, FileData, UploadDate, UploadedBy
)
VALUES
(
    @DecisionId, @DecisionDetailId, @FileName, @FileType, @FileSize,
    @FilePath, @FileData, GETDATE(), @UploadedBy
);
SELECT SCOPE_IDENTITY();";
                cmd.Parameters.Add("@DecisionId", SqlDbType.Int).Value = attachment.DecisionId;
                cmd.Parameters.Add("@DecisionDetailId", SqlDbType.Int).Value =
                    attachment.DecisionDetailId.HasValue ? (object)attachment.DecisionDetailId.Value : DBNull.Value;
                cmd.Parameters.AddWithValue("@FileName", attachment.FileName ?? string.Empty);
                cmd.Parameters.AddWithValue("@FileType", (object)attachment.FileType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FileSize", (object)attachment.FileSize ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FilePath", attachment.FilePath ?? string.Empty);
                cmd.Parameters.Add(new SqlParameter("@FileData", SqlDbType.VarBinary, -1)
                {
                    Value = (object)attachment.FileData ?? DBNull.Value
                });
                cmd.Parameters.AddWithValue("@UploadedBy", (object)attachment.UploadedBy ?? DBNull.Value);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void Delete(int attachmentId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM dbo.DecisionAttachments WHERE Id = @Id;";
                cmd.Parameters.AddWithValue("@Id", attachmentId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
