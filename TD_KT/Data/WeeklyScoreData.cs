using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using TD_KT.Services;

namespace TD_KT.Data
{
    public class WeeklyScoreRow
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Week { get; set; }
        public int? SoldierId { get; set; }
        public string FullName { get; set; }
        public string Rank { get; set; }
        public string Position { get; set; }
        public string OrgUnit { get; set; }
        public string ViolationContent { get; set; }
        public int ViolationLevel { get; set; }
        public string CommendationContent { get; set; }
        public DateTime? RecordDateTime { get; set; }
        public string EnteredBy { get; set; }
        public int AreaType { get; set; }
    }

    public class WeeklyScoreData
    {
        private readonly SqlDb _db;

        public WeeklyScoreData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public List<WeeklyScoreRow> GetByFilter(int year, int month, int week, int? areaType, List<string> orgUnitNames)
        {
            var list = new List<WeeklyScoreRow>();

            var names = (orgUnitNames ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var hasNames = names.Count > 0;

            using (var conn = _db.CreateConnection())
            {
                conn.Open();

                var sql = @"
SELECT ws.Id, ws.SoldierId, ws.Year, ws.Month, ws.Week, ws.AreaType,
       ws.FullName, ws.Rank, ws.Position,
       CASE
           WHEN ou.Name IS NOT NULL THEN
               ou.Name + CASE
                           WHEN parent.Name IS NOT NULL
                                AND LTRIM(RTRIM(parent.Name)) <> N''
                                AND parent.Name <> N'Chấm điểm thi đua'
                           THEN N' - ' + parent.Name
                           ELSE N''
                        END
           ELSE ws.OrgUnit
       END AS OrgUnit,
       ws.ViolationContent, ws.ViolationLevel, ws.CommendationContent, ws.RecordDateTime, ws.EnteredBy
FROM dbo.WeeklyScores ws
LEFT JOIN dbo.Soldiers s ON s.Id = ws.SoldierId
LEFT JOIN dbo.OrgUnits ou ON ou.Id = s.OrgUnitId
LEFT JOIN dbo.OrgUnits parent ON parent.Id = ou.ParentId
WHERE ws.Year=@Year AND (@Month<=0 OR ws.Month=@Month) AND (@Week<=0 OR ws.Week=@Week)";

                if (areaType.HasValue)
                    sql += " AND ws.AreaType=@AreaType";

                if (hasNames)
                {
                    // IN (@n0,@n1,...)
                    var inParams = string.Join(",", names.Select((_, i) => "@n" + i));
                    sql += $" AND ws.OrgUnit IN ({inParams})";
                }

                sql += " ORDER BY ws.RecordDateTime DESC";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);
                    cmd.Parameters.AddWithValue("@Week", week);

                    if (areaType.HasValue)
                        cmd.Parameters.AddWithValue("@AreaType", areaType.Value);

                    if (hasNames)
                    {
                        for (int i = 0; i < names.Count; i++)
                            cmd.Parameters.AddWithValue("@n" + i, names[i]);
                    }

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            list.Add(new WeeklyScoreRow
                            {
                                Id = Convert.ToInt32(rd["Id"]),
                                Year = rd["Year"] == DBNull.Value ? 0 : Convert.ToInt32(rd["Year"]),
                                Month = rd["Month"] == DBNull.Value ? 0 : Convert.ToInt32(rd["Month"]),
                                Week = rd["Week"] == DBNull.Value ? 0 : Convert.ToInt32(rd["Week"]),
                                SoldierId = rd["SoldierId"] == DBNull.Value ? (int?)null : Convert.ToInt32(rd["SoldierId"]),
                                AreaType = rd["AreaType"] == DBNull.Value ? 0 : Convert.ToInt32(rd["AreaType"]),
                                FullName = rd["FullName"] as string,
                                Rank = rd["Rank"] as string,
                                Position = rd["Position"] as string,
                                OrgUnit = rd["OrgUnit"] as string,
                                ViolationContent = rd["ViolationContent"] as string,
                                ViolationLevel = rd["ViolationLevel"] == DBNull.Value ? 0 : Convert.ToInt32(rd["ViolationLevel"]),
                                CommendationContent = rd["CommendationContent"] as string,
                                RecordDateTime = rd["RecordDateTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["RecordDateTime"]),
                                EnteredBy = rd["EnteredBy"] as string,
                            });
                        }
                    }
                }
            }

            return list;
        }

        public int Insert(int year, int month, int week, int areaType,
            int? soldierId,
            string fullName,
            string rank,
            string position,
            string orgUnit,
            string violationContent,
            int violationLevel,
            string commendationContent,
            DateTime? recordDateTime,
            string enteredBy)
        {
            using (var conn = _db.CreateConnection())
            {
                conn.Open();

                var sql = @"
INSERT INTO dbo.WeeklyScores
    (Year, Month, Week, AreaType, SoldierId, FullName, Rank, Position, OrgUnit,
     ViolationContent, ViolationLevel, CommendationContent, RecordDateTime, EnteredBy)
VALUES
    (@Year, @Month, @Week, @AreaType, @SoldierId, @FullName, @Rank, @Position, @OrgUnit,
     @ViolationContent, @ViolationLevel, @CommendationContent, @RecordDateTime, @EnteredBy);
SELECT SCOPE_IDENTITY();";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);
                    cmd.Parameters.AddWithValue("@Week", week);
                    cmd.Parameters.AddWithValue("@AreaType", areaType);

                    cmd.Parameters.AddWithValue("@SoldierId", (object)soldierId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FullName", (object)(fullName ?? string.Empty));
                    cmd.Parameters.AddWithValue("@Rank", (object)rank ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Position", (object)position ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@OrgUnit", (object)orgUnit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ViolationContent", (object)violationContent ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ViolationLevel", violationLevel);
                    cmd.Parameters.AddWithValue("@CommendationContent", (object)commendationContent ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RecordDateTime", (object)recordDateTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EnteredBy", (object)enteredBy ?? DBNull.Value);

                    var idObj = cmd.ExecuteScalar();
                    return Convert.ToInt32(idObj);
                }
            }
        }

        public void Update(int id,
            int areaType,
            int? soldierId,
            string fullName,
            string rank,
            string position,
            string orgUnit,
            string violationContent,
            int violationLevel,
            string commendationContent,
            DateTime? recordDateTime,
            string enteredBy)
        {
            using (var conn = _db.CreateConnection())
            {
                conn.Open();

                var sql = @"
UPDATE dbo.WeeklyScores
SET AreaType=@AreaType,
    SoldierId=@SoldierId,
    FullName=@FullName,
    Rank=@Rank,
    Position=@Position,
    OrgUnit=@OrgUnit,
    ViolationContent=@ViolationContent,
    ViolationLevel=@ViolationLevel,
    CommendationContent=@CommendationContent,
    RecordDateTime=@RecordDateTime,
    EnteredBy=@EnteredBy
WHERE Id=@Id;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@AreaType", areaType);
                    cmd.Parameters.AddWithValue("@SoldierId", (object)soldierId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FullName", (object)(fullName ?? string.Empty));
                    cmd.Parameters.AddWithValue("@Rank", (object)rank ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Position", (object)position ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@OrgUnit", (object)orgUnit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ViolationContent", (object)violationContent ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ViolationLevel", violationLevel);
                    cmd.Parameters.AddWithValue("@CommendationContent", (object)commendationContent ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RecordDateTime", (object)recordDateTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EnteredBy", (object)enteredBy ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var conn = _db.CreateConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand("DELETE FROM dbo.WeeklyScores WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}