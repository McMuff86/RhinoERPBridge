using System;
using System.Collections.Generic;
using System.Data.Odbc;
using Dapper;
using RhinoERPBridge.Models;
using RhinoERPBridge.Services;

namespace RhinoERPBridge.Data
{
    public class DynamicResult
    {
        public List<string> Columns { get; } = new List<string>();
        public List<Dictionary<string, object>> Rows { get; } = new List<Dictionary<string, object>>();
    }

    public static class DynamicSqlHelper
    {
        public static DynamicResult QueryTop(DbSettings s, string term, int top)
        {
            var like = "%" + (term ?? string.Empty) + "%";
            var safeTop = Math.Max(1, Math.Min(top, 10000));
            var sql = $"SELECT TOP ({safeTop}) * FROM {s.ArticlesTable} WHERE (? = '' OR {s.ColName} LIKE ? OR {s.ColSku} LIKE ? OR {s.ColDescription} LIKE ? OR {s.ColCategory} LIKE ?) ORDER BY {s.ColName}";

            var drivers = new[] { "{ODBC Driver 18 for SQL Server}", "{ODBC Driver 17 for SQL Server}", "{SQL Server}" };
            foreach (var driver in drivers)
            {
                try
                {
                    using (var conn = new OdbcConnection(BuildOdbcConnectionStringForDriver(s, driver)))
                    {
                        conn.Open();
                        var dp = new DynamicParameters();
                        dp.Add("p1", term ?? string.Empty);
                        dp.Add("p2", like);
                        dp.Add("p3", like);
                        dp.Add("p4", like);
                        dp.Add("p5", like);
                        var rows = conn.Query(sql, dp, commandTimeout: 15);

                        var result = new DynamicResult();
                        foreach (IDictionary<string, object> r in rows)
                        {
                            if (result.Columns.Count == 0)
                                result.Columns.AddRange(r.Keys);
                            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            foreach (var kv in r)
                                dict[kv.Key] = kv.Value;
                            result.Rows.Add(dict);
                        }
                        return result;
                    }
                }
                catch
                {
                    // try next driver
                }
            }
            throw new Exception("Dynamic query failed for all ODBC drivers.");
        }

        private static string BuildOdbcConnectionStringForDriver(DbSettings s, string driver)
        {
            var parts = new List<string>
            {
                $"Driver={driver}",
                $"Server={s.Server}",
                $"Database={s.Database}",
                $"TrustServerCertificate={(s.TrustServerCertificate ? "Yes" : "No")}"
            };
            if (s.AuthMode == DbAuthMode.Windows)
                parts.Add("Trusted_Connection=Yes");
            else
            {
                var pwd = SettingsService.Decrypt(s.EncryptedPassword);
                parts.Add($"Uid={s.Username}");
                parts.Add($"Pwd={pwd}");
            }
            return string.Join(";", parts);
        }
    }
}


