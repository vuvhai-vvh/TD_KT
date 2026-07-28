using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TD_KT.Services;
using TD_KT.ViewModels;

namespace TD_KT.Data
{
    public class SoldierData
    {
        private readonly SqlDb _db;

        public SoldierData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<SoldierDisplayModel> GetAllDisplay()
        {
            var result = new List<SoldierDisplayModel>();
            const string sql = @"
SELECT 
    s.Id,
    s.FullName,
    s.SubjectGroup,
    s.RankId,
    r.Name AS RankName,
    s.PositionId,
    p.Name AS PositionName,
    s.OrgUnitId,
    ou.ParentId AS OrgUnitParentId,
    ou.Name AS OrgUnitName,
    parent.Name AS OrgUnitParentName,
    s.EnlistmentDate
    ,s.BirthYear
    ,s.Hometown
    ,s.CitizenId
FROM dbo.Soldiers s
LEFT JOIN dbo.Ranks r ON r.Id = s.RankId
LEFT JOIN dbo.Positions p ON p.Id = s.PositionId
LEFT JOIN dbo.OrgUnits ou ON ou.Id = s.OrgUnitId
LEFT JOIN dbo.OrgUnits parent ON parent.Id = ou.ParentId
ORDER BY s.FullName;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    var stt = 1;
                    while (r.Read())
                    {
                        DateTime? enlist = null;
                        var ord = r.GetOrdinal("EnlistmentDate");
                        if (!r.IsDBNull(ord)) enlist = r.GetDateTime(ord);

                        var years = 0;
                        if (enlist.HasValue)
                        {
                            var now = DateTime.Today;
                            years = now.Year - enlist.Value.Year;
                            if (enlist.Value.Date > now.AddYears(-years)) years--;
                            if (years < 0) years = 0;
                        }


                        var orgUnitName = SqlDb.GetString(r, "OrgUnitName");
                        var orgUnitParentName = SqlDb.GetString(r, "OrgUnitParentName");
                        // Hiển thị dạng: "Đơn vị con - Đơn vị cha" (không hiển thị root nội bộ "Chấm điểm thi đua")
                        var orgUnitDisplay = orgUnitName;
                        if (!string.IsNullOrWhiteSpace(orgUnitParentName)
                            && !string.Equals(orgUnitParentName.Trim(), "Chấm điểm thi đua", StringComparison.OrdinalIgnoreCase))
                        {
                            orgUnitDisplay = $"{orgUnitName} - {orgUnitParentName}";
                        }

                        result.Add(new SoldierDisplayModel
                        {
                            Stt = stt++,
                            Id = SqlDb.GetInt(r, "Id"),
                            FullName = SqlDb.GetString(r, "FullName"),
                            SubjectGroup = SqlDb.GetString(r, "SubjectGroup"),
                            RankId = SqlDb.GetNullableInt(r, "RankId"),
                            Rank = SqlDb.GetString(r, "RankName"),
                            PositionId = SqlDb.GetNullableInt(r, "PositionId"),
                            Position = SqlDb.GetString(r, "PositionName"),
                            OrgUnitId = SqlDb.GetNullableInt(r, "OrgUnitId"),
                            OrgUnitParentId = SqlDb.GetNullableInt(r, "OrgUnitParentId"),
                            OrgUnit = orgUnitDisplay,
                            BirthYear = SqlDb.GetNullableInt(r, "BirthYear"),
                            Hometown = SqlDb.GetString(r, "Hometown"),
                            CitizenId = SqlDb.GetString(r, "CitizenId"),
                            EnlistmentDate = enlist,
                            YearsOfService = years
                        });
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Insert đầy đủ theo DB v3 (Soldiers có BirthYear + EnlistmentDate + Hometown).
        /// </summary>
        public void Insert(string fullName, string subjectGroup, int? rankId, int? positionId, int? orgUnitId,
            int? birthYear, DateTime? enlistmentDate, string hometown, string citizenId)
        {
            const string sql = @"
INSERT INTO dbo.Soldiers(FullName, SubjectGroup, RankId, PositionId, OrgUnitId, BirthYear, EnlistmentDate, Hometown, CitizenId)
VALUES(@FullName, @SubjectGroup, @RankId, @PositionId, @OrgUnitId, @BirthYear, @EnlistmentDate, @Hometown, @CitizenId);";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@FullName", fullName ?? string.Empty);
                cmd.Parameters.AddWithValue("@SubjectGroup", subjectGroup ?? string.Empty);
                cmd.Parameters.AddWithValue("@RankId", (object)rankId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@PositionId", (object)positionId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@OrgUnitId", (object)orgUnitId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@BirthYear", (object)birthYear ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@EnlistmentDate", (object)enlistmentDate ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@Hometown", hometown ?? string.Empty);
                cmd.Parameters.AddWithValue("@CitizenId", citizenId ?? string.Empty);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Overload để tương thích code cũ/VM chưa truyền EnlistmentDate.
        /// (Fix lỗi CS7036: thiếu tham số enlistmentDate)
        /// </summary>
        public void Insert(string fullName, string subjectGroup, int? rankId, int? positionId, int? orgUnitId,
            int? birthYear, string hometown, string citizenId)
        {
            Insert(fullName, subjectGroup, rankId, positionId, orgUnitId, birthYear, null, hometown, citizenId);
        }

        /// <summary>
        /// Overload tương thích chữ ký cũ (chỉ có EnlistmentDate).
        /// </summary>
        public void Insert(string fullName, string subjectGroup, int? rankId, int? positionId, int? orgUnitId,
            DateTime? enlistmentDate)
        {
            Insert(fullName, subjectGroup, rankId, positionId, orgUnitId, null, enlistmentDate, string.Empty, string.Empty);
        }

        /// <summary>
        /// Update đầy đủ theo DB v3.
        /// </summary>
        public void Update(int id, string fullName, string subjectGroup, int? rankId, int? positionId, int? orgUnitId,
            int? birthYear, DateTime? enlistmentDate, string hometown, string citizenId)
        {
            const string sql = @"
UPDATE dbo.Soldiers
SET FullName=@FullName,
    SubjectGroup=@SubjectGroup,
    RankId=@RankId,
    PositionId=@PositionId,
    OrgUnitId=@OrgUnitId,
    BirthYear=@BirthYear,
    EnlistmentDate=@EnlistmentDate,
    Hometown=@Hometown,
    CitizenId=@CitizenId
WHERE Id=@Id;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@FullName", fullName ?? string.Empty);
                cmd.Parameters.AddWithValue("@SubjectGroup", subjectGroup ?? string.Empty);
                cmd.Parameters.AddWithValue("@RankId", (object)rankId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@PositionId", (object)positionId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@OrgUnitId", (object)orgUnitId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@BirthYear", (object)birthYear ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@EnlistmentDate", (object)enlistmentDate ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@Hometown", hometown ?? string.Empty);
                cmd.Parameters.AddWithValue("@CitizenId", citizenId ?? string.Empty);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Overload để tương thích code cũ/VM chưa truyền EnlistmentDate.
        /// (Fix lỗi CS7036: thiếu tham số enlistmentDate)
        /// </summary>
        public void Update(int id, string fullName, string subjectGroup, int? rankId, int? positionId, int? orgUnitId,
            int? birthYear, string hometown, string citizenId)
        {
            Update(id, fullName, subjectGroup, rankId, positionId, orgUnitId, birthYear, null, hometown, citizenId);
        }

        /// <summary>
        /// Overload tương thích chữ ký cũ (chỉ có EnlistmentDate).
        /// </summary>
        public void Update(int id, string fullName, string subjectGroup, int? rankId, int? positionId, int? orgUnitId,
            DateTime? enlistmentDate)
        {
            Update(id, fullName, subjectGroup, rankId, positionId, orgUnitId, null, enlistmentDate, string.Empty, string.Empty);
        }

        public void Delete(int id)
        {
            const string sql = "DELETE FROM dbo.Soldiers WHERE Id=@Id;";
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
