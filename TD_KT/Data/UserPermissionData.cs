using System.Collections.Generic;
using System.Data.SqlClient;
using TD_KT.Services;
using TD_KT.ViewModels;

namespace TD_KT.Data
{
    public class UserPermissionFlags
    {
        public bool ViewTM { get; set; } = true;
        public bool EditTM { get; set; }
        public bool ViewCT { get; set; } = true;
        public bool EditCT { get; set; }
        public bool ViewHCKT { get; set; } = true;
        public bool EditHCKT { get; set; }
        public bool ViewNV { get; set; } = true;
        public bool EditNV { get; set; }
        public bool CanAccessReport { get; set; } = true;
        public bool CanAccessAccountTab { get; set; }
        public bool CanAccessPermissionTab { get; set; }
        public bool CanFinalizeScore { get; set; }
        public bool CanLockWeek { get; set; }
    }
    public class UserPermissionData
    {
        private readonly SqlDb _db;

        public UserPermissionData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<UserPermissionDisplayModel> GetUserPermissionsDisplay()
        {
            var result = new List<UserPermissionDisplayModel>();
            EnsurePermissionColumns();
            const string sql = @"
SELECT 
    u.Id AS UserId,
    u.Username,
    ou.Name AS OrgUnit,
    p.Name AS Position,
    up.ViewTM,
    up.EditTM,
    up.ViewCT,
    up.EditCT,
    up.ViewHCKT,
    up.EditHCKT,
    up.ViewNV,
    up.EditNV,
    up.CanFinalizeScore
FROM dbo.Users u
LEFT JOIN dbo.OrgUnits ou ON ou.Id = u.OrgUnitId
LEFT JOIN dbo.Positions p ON p.Id = u.PositionId
LEFT JOIN dbo.UserPermissions up ON up.UserId = u.Id
WHERE LTRIM(RTRIM(u.Role)) = N'User'
ORDER BY u.Username;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    var stt = 1;
                    while (r.Read())
                    {
                        // If no row in UserPermissions (left join null) use DB defaults:
                        // View* default 1, Edit* default 0, CanFinalize default 0
                        bool viewTM = r.IsDBNull(r.GetOrdinal("ViewTM")) ? true : SqlDb.GetBool(r, "ViewTM");
                        bool editTM = r.IsDBNull(r.GetOrdinal("EditTM")) ? false : SqlDb.GetBool(r, "EditTM");
                        bool viewCT = r.IsDBNull(r.GetOrdinal("ViewCT")) ? true : SqlDb.GetBool(r, "ViewCT");
                        bool editCT = r.IsDBNull(r.GetOrdinal("EditCT")) ? false : SqlDb.GetBool(r, "EditCT");
                        bool viewHCKT = r.IsDBNull(r.GetOrdinal("ViewHCKT")) ? true : SqlDb.GetBool(r, "ViewHCKT");
                        bool editHCKT = r.IsDBNull(r.GetOrdinal("EditHCKT")) ? false : SqlDb.GetBool(r, "EditHCKT");
                        bool viewNV = r.IsDBNull(r.GetOrdinal("ViewNV")) ? true : SqlDb.GetBool(r, "ViewNV");
                        bool editNV = r.IsDBNull(r.GetOrdinal("EditNV")) ? false : SqlDb.GetBool(r, "EditNV");
                        bool canFinalizeScore = r.IsDBNull(r.GetOrdinal("CanFinalizeScore")) ? false : SqlDb.GetBool(r, "CanFinalizeScore");
                        result.Add(new UserPermissionDisplayModel
                        {
                            Stt = stt++,
                            UserId = SqlDb.GetInt(r, "UserId"),
                            Username = SqlDb.GetString(r, "Username"),
                            OrgUnit = SqlDb.GetString(r, "OrgUnit"),
                            Position = SqlDb.GetString(r, "Position"),
                            ViewTM = viewTM,
                            EditTM = editTM,
                            ViewCT = viewCT,
                            EditCT = editCT,
                            ViewHCKT = viewHCKT,
                            EditHCKT = editHCKT,
                            ViewNV = viewNV,
                            EditNV = editNV,
                            CanFinalizeScore = canFinalizeScore
                        });
                    }
                }
            }
            return result;
        }

        public void EnsureUserPermissionsSeeded()
        {
            EnsurePermissionColumns();
            const string sql = @"
-- Admin toàn quyền
INSERT INTO dbo.UserPermissions
    (UserId, ViewTM, EditTM, ViewCT, EditCT, ViewHCKT, EditHCKT, ViewNV, EditNV, CanFinalizeScore, CanAccessReport, CanAccessAdmin, CanAccessAccountTab, CanAccessPermissionTab, CanLockWeek)
SELECT u.Id, 1,1, 1,1, 1,1, 1,1, 1, 1, 1, 1, 1, 0
FROM dbo.Users u
WHERE u.Role = N'Admin'
  AND NOT EXISTS (SELECT 1 FROM dbo.UserPermissions p WHERE p.UserId = u.Id);

-- User mặc định: xem, không sửa
INSERT INTO dbo.UserPermissions
    (UserId, ViewTM, EditTM, ViewCT, EditCT, ViewHCKT, EditHCKT, ViewNV, EditNV, CanFinalizeScore, CanAccessReport, CanAccessAdmin, CanAccessAccountTab, CanAccessPermissionTab, CanLockWeek)
SELECT u.Id, 1,0, 1,0, 1,0, 1,0, 0, 1, 0, 0, 0, 0
FROM dbo.Users u
WHERE u.Role = N'User'
  AND NOT EXISTS (SELECT 1 FROM dbo.UserPermissions p WHERE p.UserId = u.Id);

-- Báo cáo - Thống kê là chức năng dùng chung cho mọi tài khoản.
UPDATE dbo.UserPermissions
SET CanAccessReport = 1
WHERE CanAccessReport = 0;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<AdminPermissionDisplayModel> GetAdminPermissionsDisplay()
        {
            var result = new List<AdminPermissionDisplayModel>();
            EnsurePermissionColumns();
            const string sql = @"
SELECT 
    u.Id AS UserId,
    u.Username,
    ou.Name AS OrgUnit,
    p.Name AS Position,
    up.CanAccessAdmin,
    up.CanAccessAccountTab,
    up.CanAccessPermissionTab,
    up.CanFinalizeScore,
    up.CanLockWeek
FROM dbo.Users u
LEFT JOIN dbo.OrgUnits ou ON ou.Id = u.OrgUnitId
LEFT JOIN dbo.Positions p ON p.Id = u.PositionId
LEFT JOIN dbo.UserPermissions up ON up.UserId = u.Id
WHERE LTRIM(RTRIM(u.Role)) = N'Admin'
ORDER BY u.Username;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    var stt = 1;
                    while (r.Read())
                    {
                        bool canAdminLegacy = r.IsDBNull(r.GetOrdinal("CanAccessAdmin")) ? false : SqlDb.GetBool(r, "CanAccessAdmin");
                        bool canAccessAccountTab = r.IsDBNull(r.GetOrdinal("CanAccessAccountTab")) ? canAdminLegacy : SqlDb.GetBool(r, "CanAccessAccountTab");
                        bool canAccessPermissionTab = r.IsDBNull(r.GetOrdinal("CanAccessPermissionTab")) ? canAdminLegacy : SqlDb.GetBool(r, "CanAccessPermissionTab");
                        bool canFinalize = r.IsDBNull(r.GetOrdinal("CanFinalizeScore")) ? false : SqlDb.GetBool(r, "CanFinalizeScore");
                        bool canLockWeek = r.IsDBNull(r.GetOrdinal("CanLockWeek")) ? false : SqlDb.GetBool(r, "CanLockWeek");

                        result.Add(new AdminPermissionDisplayModel
                        {
                            Stt = stt++,
                            UserId = SqlDb.GetInt(r, "UserId"),
                            Username = SqlDb.GetString(r, "Username"),
                            OrgUnit = SqlDb.GetString(r, "OrgUnit"),
                            Position = SqlDb.GetString(r, "Position"),
                            CanAccessAccountTab = canAccessAccountTab,
                            CanAccessPermissionTab = canAccessPermissionTab,
                            CanFinalizeScore = canFinalize,
                            CanLockWeek = canLockWeek
                        });
                    }
                }
            }
            return result;
        }

        public void UpsertUserPermissions(UserPermissionDisplayModel m)
        {
            EnsurePermissionColumns();
            const string sql = @"
MERGE dbo.UserPermissions AS target
USING (SELECT @UserId AS UserId) AS source
ON target.UserId = source.UserId
WHEN MATCHED THEN
  UPDATE SET 
    ViewTM=@ViewTM, EditTM=@EditTM,
    ViewCT=@ViewCT, EditCT=@EditCT,
    ViewHCKT=@ViewHCKT, EditHCKT=@EditHCKT,
    ViewNV=@ViewNV, EditNV=@EditNV,
    CanFinalizeScore=@CanFinalizeScore,
    CanLockWeek=0
WHEN NOT MATCHED THEN
  INSERT (UserId, ViewTM, EditTM, ViewCT, EditCT, ViewHCKT, EditHCKT, ViewNV, EditNV, CanFinalizeScore, CanAccessReport, CanAccessAdmin, CanAccessAccountTab, CanAccessPermissionTab, CanLockWeek)
  VALUES (@UserId, @ViewTM, @EditTM, @ViewCT, @EditCT, @ViewHCKT, @EditHCKT, @ViewNV, @EditNV, @CanFinalizeScore, 1, 0, 0, 0, 0);";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", m.UserId);
                cmd.Parameters.AddWithValue("@ViewTM", m.ViewTM);
                cmd.Parameters.AddWithValue("@EditTM", m.EditTM);
                cmd.Parameters.AddWithValue("@ViewCT", m.ViewCT);
                cmd.Parameters.AddWithValue("@EditCT", m.EditCT);
                cmd.Parameters.AddWithValue("@ViewHCKT", m.ViewHCKT);
                cmd.Parameters.AddWithValue("@EditHCKT", m.EditHCKT);
                cmd.Parameters.AddWithValue("@ViewNV", m.ViewNV);
                cmd.Parameters.AddWithValue("@EditNV", m.EditNV);
                cmd.Parameters.AddWithValue("@CanFinalizeScore", m.CanFinalizeScore);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }


        public void UpsertAdminPermissions(AdminPermissionDisplayModel m)
        {
            EnsurePermissionColumns();
            const string sql = @"
MERGE dbo.UserPermissions AS target
USING (SELECT @UserId AS UserId) AS source
ON target.UserId = source.UserId
WHEN MATCHED THEN
  UPDATE SET 
    CanAccessAccountTab=@CanAccessAccountTab,
    CanAccessPermissionTab=@CanAccessPermissionTab,
    CanFinalizeScore=@CanFinalizeScore,
    CanLockWeek=@CanLockWeek
WHEN NOT MATCHED THEN
  INSERT (UserId, ViewTM, EditTM, ViewCT, EditCT, ViewHCKT, EditHCKT, ViewNV, EditNV, CanFinalizeScore, CanAccessReport, CanAccessAdmin, CanAccessAccountTab, CanAccessPermissionTab, CanLockWeek)
  VALUES (@UserId, 1,0,1,0,1,0,1,0, @CanFinalizeScore, 1, 0, @CanAccessAccountTab, @CanAccessPermissionTab, @CanLockWeek);";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", m.UserId);
                cmd.Parameters.AddWithValue("@CanAccessAccountTab", m.CanAccessAccountTab);
                cmd.Parameters.AddWithValue("@CanAccessPermissionTab", m.CanAccessPermissionTab);
                cmd.Parameters.AddWithValue("@CanFinalizeScore", m.CanFinalizeScore);
                cmd.Parameters.AddWithValue("@CanLockWeek", m.CanLockWeek);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public UserPermissionFlags GetUserPermissionFlags(int userId)
        {
            EnsurePermissionColumns();
            const string sql = @"
SELECT ViewTM, EditTM, ViewCT, EditCT, ViewHCKT, EditHCKT, ViewNV, EditNV,
       CanAccessReport, CanAccessAdmin, CanAccessAccountTab, CanAccessPermissionTab, CanFinalizeScore, CanLockWeek
FROM dbo.UserPermissions
WHERE UserId = @UserId;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return new UserPermissionFlags
                        {
                            ViewTM = r.IsDBNull(r.GetOrdinal("ViewTM")) ? true : SqlDb.GetBool(r, "ViewTM"),
                            EditTM = r.IsDBNull(r.GetOrdinal("EditTM")) ? false : SqlDb.GetBool(r, "EditTM"),
                            ViewCT = r.IsDBNull(r.GetOrdinal("ViewCT")) ? true : SqlDb.GetBool(r, "ViewCT"),
                            EditCT = r.IsDBNull(r.GetOrdinal("EditCT")) ? false : SqlDb.GetBool(r, "EditCT"),
                            ViewHCKT = r.IsDBNull(r.GetOrdinal("ViewHCKT")) ? true : SqlDb.GetBool(r, "ViewHCKT"),
                            EditHCKT = r.IsDBNull(r.GetOrdinal("EditHCKT")) ? false : SqlDb.GetBool(r, "EditHCKT"),
                            ViewNV = r.IsDBNull(r.GetOrdinal("ViewNV")) ? true : SqlDb.GetBool(r, "ViewNV"),
                            EditNV = r.IsDBNull(r.GetOrdinal("EditNV")) ? false : SqlDb.GetBool(r, "EditNV"),
                            CanAccessReport = true,
                            CanAccessAccountTab = r.IsDBNull(r.GetOrdinal("CanAccessAccountTab"))
                                ? (r.IsDBNull(r.GetOrdinal("CanAccessAdmin")) ? false : SqlDb.GetBool(r, "CanAccessAdmin"))
                                : SqlDb.GetBool(r, "CanAccessAccountTab"),
                            CanAccessPermissionTab = r.IsDBNull(r.GetOrdinal("CanAccessPermissionTab"))
                                ? (r.IsDBNull(r.GetOrdinal("CanAccessAdmin")) ? false : SqlDb.GetBool(r, "CanAccessAdmin"))
                                : SqlDb.GetBool(r, "CanAccessPermissionTab"),
                            CanFinalizeScore = r.IsDBNull(r.GetOrdinal("CanFinalizeScore")) ? false : SqlDb.GetBool(r, "CanFinalizeScore"),
                            CanLockWeek = r.IsDBNull(r.GetOrdinal("CanLockWeek")) ? false : SqlDb.GetBool(r, "CanLockWeek"),
                        };
                    }
                }
            }

            var role = GetUserRole(userId);
            if (string.Equals(role, "Admin", System.StringComparison.OrdinalIgnoreCase))
            {
                return new UserPermissionFlags
                {
                    CanAccessReport = true,
                    CanAccessAccountTab = true,
                    CanAccessPermissionTab = true,
                    CanFinalizeScore = true,
                    CanLockWeek = false,
                    ViewTM = true,
                    ViewCT = true,
                    ViewHCKT = true,
                    ViewNV = true
                };
            }

            return new UserPermissionFlags
            {
                CanAccessReport = true,
                CanAccessAccountTab = false,
                CanAccessPermissionTab = false,
                CanFinalizeScore = false,
                CanLockWeek = false
            };
        }

        private string GetUserRole(int userId)
        {
            const string sql = "SELECT Role FROM dbo.Users WHERE Id = @UserId;";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                var roleObj = cmd.ExecuteScalar();
                return roleObj?.ToString();
            }
        }

        private void EnsurePermissionColumns()
        {
            const string sql = @"
IF COL_LENGTH('dbo.UserPermissions', 'CanLockWeek') IS NULL
BEGIN
    ALTER TABLE dbo.UserPermissions
    ADD CanLockWeek BIT NOT NULL CONSTRAINT DF_UserPermissions_CanLockWeek DEFAULT(0);
END;

IF COL_LENGTH('dbo.UserPermissions', 'CanAccessAccountTab') IS NULL
BEGIN
    ALTER TABLE dbo.UserPermissions
    ADD CanAccessAccountTab BIT NULL;
END;

IF COL_LENGTH('dbo.UserPermissions', 'CanAccessPermissionTab') IS NULL
BEGIN
    ALTER TABLE dbo.UserPermissions
    ADD CanAccessPermissionTab BIT NULL;    
END;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}