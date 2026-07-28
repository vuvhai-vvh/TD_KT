using System;
using System.Data.SqlClient;
using TD_KT.Services;

namespace TD_KT.Data
{
    public class WeekLockData
    {
        private readonly SqlDb _db;

        public WeekLockData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        private void EnsureTable()
        {
            const string sql = @"
IF OBJECT_ID('dbo.WeekLocks', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WeekLocks (
        [Year] INT NOT NULL,
        [Month] INT NOT NULL,
        [Week] INT NOT NULL,
        LockedAt DATETIME NULL,
        LockedBy NVARCHAR(50) NULL,
        CONSTRAINT PK_WeekLocks PRIMARY KEY ([Year], [Month], [Week])
    );
END";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public bool IsLocked(int year, int month, int week)
        {
            EnsureTable();
            const string sql = @"
SELECT COUNT(1)
FROM dbo.WeekLocks
WHERE [Year]=@Year AND [Month]=@Month AND [Week]=@Week;";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Year", year);
                cmd.Parameters.AddWithValue("@Month", month);
                cmd.Parameters.AddWithValue("@Week", week);
                conn.Open();
                var count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        public void LockWeek(int year, int month, int week, string lockedBy)
        {
            EnsureTable();
            const string sql = @"
MERGE dbo.WeekLocks AS target
USING (SELECT @Year AS [Year], @Month AS [Month], @Week AS [Week]) AS source
ON target.[Year] = source.[Year] AND target.[Month] = source.[Month] AND target.[Week] = source.[Week]
WHEN MATCHED THEN
    UPDATE SET LockedAt = @LockedAt, LockedBy = @LockedBy
WHEN NOT MATCHED THEN
    INSERT ([Year], [Month], [Week], LockedAt, LockedBy)
    VALUES (@Year, @Month, @Week, @LockedAt, @LockedBy);";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Year", year);
                cmd.Parameters.AddWithValue("@Month", month);
                cmd.Parameters.AddWithValue("@Week", week);
                cmd.Parameters.AddWithValue("@LockedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@LockedBy", (object)lockedBy ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}