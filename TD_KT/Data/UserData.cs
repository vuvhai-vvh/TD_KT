using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TD_KT.Services;
using TD_KT.ViewModels;

namespace TD_KT.Data
{
    public class UserData
    {
        private readonly SqlDb _db;

        private static string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return role;
            var r = role.Trim();
            // D? li?u c? trong DB: N"Ng??i dùng" -> hi?n th?/chu?n hóa thành "User"
            if (r.Equals("Ng??i dùng", StringComparison.OrdinalIgnoreCase))
                return "User";
            return r;
        }

        public UserData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<UserDisplayModel> GetAllDisplay()
        {
            var result = new List<UserDisplayModel>();
            const string sql = @"
SELECT 
    u.Id,
    u.Username,
    u.DisplayName,
    u.Role,
    u.IsActive,
    ou.Name AS OrgUnit,
    p.Name  AS Position
FROM dbo.Users u
LEFT JOIN dbo.OrgUnits ou ON ou.Id = u.OrgUnitId
LEFT JOIN dbo.Positions p ON p.Id = u.PositionId
ORDER BY u.Id DESC;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    var stt = 1;
                    while (r.Read())
                    {
                        result.Add(new UserDisplayModel
                        {
                            Stt = stt++,
                            Id = SqlDb.GetInt(r, "Id"),
                            Username = SqlDb.GetString(r, "Username"),
                            DisplayName = SqlDb.GetString(r, "DisplayName"),
                            Role = NormalizeRole(SqlDb.GetString(r, "Role")),
                            IsActive = SqlDb.GetBool(r, "IsActive"),
                            OrgUnit = SqlDb.GetString(r, "OrgUnit"),
                            Position = SqlDb.GetString(r, "Position")
                        });
                    }
                }
            }
            return result;
        }

        public List<(int UserId, string Username, string OrgUnit, string Position)> GetUsersByRole(string role)
        {
            var result = new List<(int, string, string, string)>();
            const string sql = @"
SELECT u.Id AS UserId, u.Username, ou.Name AS OrgUnit, p.Name AS Position
FROM dbo.Users u
LEFT JOIN dbo.OrgUnits ou ON ou.Id = u.OrgUnitId
LEFT JOIN dbo.Positions p ON p.Id = u.PositionId
WHERE u.Role = @Role
ORDER BY u.Username;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Role", role ?? string.Empty);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add((
                            SqlDb.GetInt(r, "UserId"),
                            SqlDb.GetString(r, "Username"),
                            SqlDb.GetString(r, "OrgUnit"),
                            SqlDb.GetString(r, "Position")
                        ));
                    }
                }
            }
            return result;
        }

        public void Insert(string username, string passwordHash, string passwordSalt, string passwordAlgo, int passwordIterations, string displayName, int orgUnitId, int positionId, string role, bool isActive)
        {
            const string sql = @"
INSERT INTO dbo.Users(Username, Password, PasswordHash, PasswordSalt, PasswordAlgo, PasswordIterations, DisplayName, OrgUnitId, PositionId, Role, IsActive)
VALUES(@Username, NULL, @PasswordHash, @PasswordSalt, @PasswordAlgo, @PasswordIterations, @DisplayName, @OrgUnitId, @PositionId, @Role, @IsActive);";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Username", username ?? string.Empty);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PasswordSalt", passwordSalt ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PasswordAlgo", passwordAlgo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PasswordIterations", passwordIterations);
                cmd.Parameters.AddWithValue("@DisplayName", displayName ?? string.Empty);
                cmd.Parameters.AddWithValue("@OrgUnitId", orgUnitId);
                cmd.Parameters.AddWithValue("@PositionId", positionId);
                cmd.Parameters.AddWithValue("@Role", role ?? string.Empty);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(int id, string username, string passwordHash, string passwordSalt, string passwordAlgo, int passwordIterations, string displayName, int orgUnitId, int positionId, string role, bool isActive)
        {
            const string sql = @"
UPDATE dbo.Users
SET Username=@Username,
    Password = CASE WHEN (@PasswordHash IS NULL OR LTRIM(RTRIM(@PasswordHash)) = '') THEN Password ELSE NULL END,
    PasswordHash = CASE WHEN (@PasswordHash IS NULL OR LTRIM(RTRIM(@PasswordHash)) = '') THEN PasswordHash ELSE @PasswordHash END,
    PasswordSalt = CASE WHEN (@PasswordSalt IS NULL OR LTRIM(RTRIM(@PasswordSalt)) = '') THEN PasswordSalt ELSE @PasswordSalt END,
    PasswordAlgo = CASE WHEN (@PasswordAlgo IS NULL OR LTRIM(RTRIM(@PasswordAlgo)) = '') THEN PasswordAlgo ELSE @PasswordAlgo END,
    PasswordIterations = CASE WHEN (@PasswordIterations IS NULL OR @PasswordIterations <= 0) THEN PasswordIterations ELSE @PasswordIterations END,
    DisplayName=@DisplayName,
    OrgUnitId=@OrgUnitId,
    PositionId=@PositionId,
    Role=@Role,
    IsActive=@IsActive
WHERE Id=@Id;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Username", username ?? string.Empty);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PasswordSalt", passwordSalt ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PasswordAlgo", passwordAlgo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PasswordIterations", passwordIterations);
                cmd.Parameters.AddWithValue("@DisplayName", displayName ?? string.Empty);
                cmd.Parameters.AddWithValue("@OrgUnitId", orgUnitId);
                cmd.Parameters.AddWithValue("@PositionId", positionId);
                cmd.Parameters.AddWithValue("@Role", role ?? string.Empty);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            const string sql = "DELETE FROM dbo.Users WHERE Id=@Id;";
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
