using System.Collections.Generic;
using System.Data.SqlClient;
using TD_KT.Models;
using TD_KT.Services;
using TD_KT.ViewModels;

namespace TD_KT.Data
{
    public class OrgUnitData
    {
        public const string InternalOrgUnitName = "Ch?m ?i?m thi ?ua";
        public const string InternalOrgUnitCode = "CDTD";

        private readonly SqlDb _db;

        public OrgUnitData(IConnectionStringProvider csProvider)
        {
            _db = new SqlDb(csProvider);
        }

        public static bool IsInternalOrgUnit(string code, string name)
        {
            return string.Equals(name?.Trim(), InternalOrgUnitName, System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(code?.Trim(), InternalOrgUnitCode, System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsInternalOrgUnit(OrgUnit orgUnit)
        {
            return orgUnit != null && IsInternalOrgUnit(orgUnit.Code, orgUnit.Name);
        }

        public static bool IsInternalOrgUnit(OrgUnitDisplayModel orgUnit)
        {
            return orgUnit != null && IsInternalOrgUnit(orgUnit.Symbol, orgUnit.Name);
        }


        public List<OrgUnit> GetAll()
        {
            var result = new List<OrgUnit>();
            const string sql = "SELECT Id, Code, Name, ParentId, Note FROM dbo.OrgUnits ORDER BY Name;";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new OrgUnit
                        {
                            Id = SqlDb.GetInt(r, "Id"),
                            Code = SqlDb.GetString(r, "Code"),
                            Name = SqlDb.GetString(r, "Name"),
                            ParentId = SqlDb.GetNullableInt(r, "ParentId"),
                            Note = SqlDb.GetString(r, "Note")
                        });
                    }
                }
            }
            return result;
        }

        public List<OrgUnitDisplayModel> GetAllDisplay()
        {
            var result = new List<OrgUnitDisplayModel>();
            const string sql = @"
SELECT ou.Id, ou.Code, ou.Name, ou.ParentId,
       parent.Name AS ParentName
FROM dbo.OrgUnits ou
LEFT JOIN dbo.OrgUnits parent ON parent.Id = ou.ParentId
ORDER BY ou.Name;";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    var stt = 1;
                    while (r.Read())
                    {
                        result.Add(new OrgUnitDisplayModel
                        {
                            Stt = stt++,
                            Id = SqlDb.GetInt(r, "Id"),
                            Symbol = SqlDb.GetString(r, "Code"),
                            Name = SqlDb.GetString(r, "Name"),
                            ParentId = SqlDb.GetNullableInt(r, "ParentId"),
                            ParentName = SqlDb.GetString(r, "ParentName")
                        });
                    }
                }
            }
            return result;
        }

        public void Insert(string code, string name, int? parentId, string note)
        {
            const string sql = @"
INSERT INTO dbo.OrgUnits(Code, Name, ParentId, Note)
VALUES(@Code, @Name, @ParentId, @Note);";

            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Code", (object)(code ?? string.Empty));
                cmd.Parameters.AddWithValue("@Name", (object)(name ?? string.Empty));
                cmd.Parameters.AddWithValue("@ParentId", (object)parentId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@Note", (object)(note ?? string.Empty));
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(int id, string code, string name, int? parentId, string note)
        {
            const string sql = @"
UPDATE dbo.OrgUnits
SET Code=@Code, Name=@Name, ParentId=@ParentId, Note=@Note
WHERE Id=@Id;";
            using (var conn = _db.CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Code", (object)(code ?? string.Empty));
                cmd.Parameters.AddWithValue("@Name", (object)(name ?? string.Empty));
                cmd.Parameters.AddWithValue("@ParentId", (object)parentId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@Note", (object)(note ?? string.Empty));
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            const string sql = "DELETE FROM dbo.OrgUnits WHERE Id=@Id;";
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