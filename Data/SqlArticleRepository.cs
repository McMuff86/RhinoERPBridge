using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using Dapper;
using RhinoERPBridge.Models;
using RhinoERPBridge.Services;

namespace RhinoERPBridge.Data
{
    public class SqlArticleRepository : IArticleRepository
    {
        private readonly DbSettings _settings;

        public SqlArticleRepository(DbSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public IReadOnlyList<Article> GetAll() => Search(string.Empty);

        public IReadOnlyList<Article> Search(string term)
        {
            var s = _settings;
            var like = "%" + (term ?? string.Empty) + "%";
            var sql = $@"SELECT TOP (100)
    CAST({s.ColSku} AS nvarchar(200)) AS Sku,
    CAST({s.ColName} AS nvarchar(200)) AS Name,
    CAST({s.ColDescription} AS nvarchar(500)) AS Description,
    CAST({s.ColUnit} AS nvarchar(50)) AS Unit,
    TRY_CAST({s.ColPrice} AS decimal(18,2)) AS Price,
    TRY_CAST({s.ColStock} AS int) AS Stock,
    CAST({s.ColCategory} AS nvarchar(100)) AS Category
FROM {s.ArticlesTable}
WHERE (? = '' OR {s.ColName} LIKE ? OR {s.ColSku} LIKE ? OR {s.ColDescription} LIKE ? OR {s.ColCategory} LIKE ?)
ORDER BY {s.ColName};";

            var drivers = new[] { "{ODBC Driver 18 for SQL Server}", "{ODBC Driver 17 for SQL Server}", "{SQL Server}" };
            var errors = new List<string>();
            foreach (var driver in drivers)
            {
                try
                {
                    using (var conn = new OdbcConnection(BuildOdbcConnectionStringForDriver(s, driver)))
                    {
                        conn.Open();
                        // ODBC requires positional '?' parameters; use DynamicParameters to preserve order
                        var dp = new DynamicParameters();
                        dp.Add("p1", term ?? string.Empty);
                        dp.Add("p2", like);
                        dp.Add("p3", like);
                        dp.Add("p4", like);
                        dp.Add("p5", like);
                        var results = conn.Query<Article>(sql, dp, commandTimeout: 15);
                        return results.AsList();
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{driver}: {ex.Message}");
                }
            }
            throw new Exception("ODBC search failed:\n" + string.Join("\n", errors));
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


