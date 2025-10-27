using System;
using System.Runtime.InteropServices;
using Eto.Forms;
using Eto.Drawing;
using Microsoft.Data.SqlClient;
using System.Data.SqlClient; // legacy fallback
using System.Data.Odbc; // ODBC fallback
using Rhino;
using RhinoERPBridge.Models;
using RhinoERPBridge.Services;

namespace RhinoERPBridge.UI
{
    [Guid("9E41B38A-5B26-4E3C-BB90-1B7E2A8C1E2F")]
    public class DbSettingsPanel : Panel
    {
        public static Guid PanelGuid => typeof(DbSettingsPanel).GUID;

        private readonly TextBox _server = new TextBox();
        private readonly TextBox _database = new TextBox();
        private readonly DropDown _auth = new DropDown { DataStore = new[] { "Windows", "SQL Login" } };
        private readonly TextBox _username = new TextBox();
        private readonly PasswordBox _password = new PasswordBox();
        private readonly CheckBox _encrypt = new CheckBox { Text = "Encrypt" };
        private readonly CheckBox _trust = new CheckBox { Text = "Trust Server Certificate" };
        private readonly Label _status = new Label { TextColor = Colors.Gray, Wrap = WrapMode.Word };

        // Article mapping UI
        private readonly TextBox _tbl = new TextBox();
        private readonly TextBox _colSku = new TextBox();
        private readonly TextBox _colName = new TextBox();
        private readonly TextBox _colDesc = new TextBox();
        private readonly TextBox _colUnit = new TextBox();
        private readonly TextBox _colPrice = new TextBox();
        private readonly TextBox _colStock = new TextBox();
        private readonly TextBox _colCategory = new TextBox();

        public DbSettingsPanel()
        {
            var s = SettingsService.Load();
            _server.Text = s.Server;
            _database.Text = s.Database;
            _auth.SelectedIndex = s.AuthMode == DbAuthMode.Windows ? 0 : 1;
            _username.Text = s.Username;
            _password.Text = SettingsService.Decrypt(s.EncryptedPassword);
            _encrypt.Checked = s.Encrypt;
            _trust.Checked = s.TrustServerCertificate;
            _tbl.Text = s.ArticlesTable;
            _colSku.Text = s.ColSku;
            _colName.Text = s.ColName;
            _colDesc.Text = s.ColDescription;
            _colUnit.Text = s.ColUnit;
            _colPrice.Text = s.ColPrice;
            _colStock.Text = s.ColStock;
            _colCategory.Text = s.ColCategory;

            _auth.SelectedIndexChanged += (s2, e2) => UpdateAuthVisibility();
            UpdateAuthVisibility();

            var btnTest = new Button { Text = "Test Connection" };
            var btnSave = new Button { Text = "Save" };
            var btnDefaults = new Button { Text = "Use PROD_DEFINITION defaults" };

            btnTest.Click += (s2, e2) => Test();
            btnSave.Click += (s2, e2) => Save();
            btnDefaults.Click += (s2, e2) => ApplyProdDefinitionDefaults();

            var layout = new DynamicLayout { Spacing = new Size(6, 6), Padding = new Padding(8) };
            layout.AddRow(new Label { Text = "Server" }, _server);
            layout.AddRow(new Label { Text = "Database" }, _database);
            layout.AddRow(new Label { Text = "Authentication" }, _auth);
            layout.AddRow(new Label { Text = "Username" }, _username);
            layout.AddRow(new Label { Text = "Password" }, _password);
            layout.AddRow(_encrypt);
            layout.AddRow(_trust);
            layout.AddSeparateRow(new Label { Text = "Articles Mapping", Font = SystemFonts.Bold() });
            layout.AddRow(new Label { Text = "Table" }, _tbl);
            layout.AddRow(new Label { Text = "SKU" }, _colSku);
            layout.AddRow(new Label { Text = "Name" }, _colName);
            layout.AddRow(new Label { Text = "Description" }, _colDesc);
            layout.AddRow(new Label { Text = "Unit" }, _colUnit);
            layout.AddRow(new Label { Text = "Price" }, _colPrice);
            layout.AddRow(new Label { Text = "Stock" }, _colStock);
            layout.AddRow(new Label { Text = "Category" }, _colCategory);
            layout.AddRow(btnDefaults);
            layout.AddRow(btnTest, btnSave);
            layout.AddRow(_status);
            Content = layout;
        }

        private void ApplyProdDefinitionDefaults()
        {
            _tbl.Text = "dbo.PROD_DEFINITION";
            _colSku.Text = "PD_NUM";           // Produktenummer
            _colName.Text = "PD_BEZ";         // Bezeichnung
            _colDesc.Text = "M_VERKAUFSTEXT"; // Verkaufstext lang (MEMO) – falls zu lang, später SUBSTRING
            _colUnit.Text = "M_VK_EINHEIT_ID"; // Einheit-ID (optional Mappingtabelle)
            _colPrice.Text = "M_PBU_VPREIS1";  // Preis 1 (anpassen, wenn andere Logik)
            _colStock.Text = "M_MINDESTBESTAND"; // Minimalbestand als Beispiel (oder Lagerbestandstabelle)
            _colCategory.Text = "M_MATGRUPPE_ID"; // Materialgruppe als Kategorie
            _status.Text = "Applied PROD_DEFINITION defaults. Don’t forget to Save.";
            _status.TextColor = Colors.DarkGoldenrod;
        }

        private void UpdateAuthVisibility()
        {
            var isWindows = _auth.SelectedIndex == 0;
            _username.Enabled = !isWindows;
            _password.Enabled = !isWindows;
        }

        private void Save()
        {
            var s = new DbSettings
            {
                Server = _server.Text?.Trim() ?? string.Empty,
                Database = _database.Text?.Trim() ?? string.Empty,
                AuthMode = _auth.SelectedIndex == 0 ? DbAuthMode.Windows : DbAuthMode.SqlLogin,
                Username = _username.Text?.Trim() ?? string.Empty,
                EncryptedPassword = SettingsService.Encrypt(_password.Text ?? string.Empty),
                Encrypt = _encrypt.Checked ?? true,
                TrustServerCertificate = _trust.Checked ?? true,
                ArticlesTable = _tbl.Text?.Trim() ?? string.Empty,
                ColSku = _colSku.Text?.Trim() ?? string.Empty,
                ColName = _colName.Text?.Trim() ?? string.Empty,
                ColDescription = _colDesc.Text?.Trim() ?? string.Empty,
                ColUnit = _colUnit.Text?.Trim() ?? string.Empty,
                ColPrice = _colPrice.Text?.Trim() ?? string.Empty,
                ColStock = _colStock.Text?.Trim() ?? string.Empty,
                ColCategory = _colCategory.Text?.Trim() ?? string.Empty
            };
            SettingsService.Save(s);
            _status.Text = "Saved.";
        }

        private void Test()
        {
            try
            {
                var s = new DbSettings
                {
                    Server = _server.Text?.Trim() ?? string.Empty,
                    Database = _database.Text?.Trim() ?? string.Empty,
                    AuthMode = _auth.SelectedIndex == 0 ? DbAuthMode.Windows : DbAuthMode.SqlLogin,
                    Username = _username.Text?.Trim() ?? string.Empty,
                    EncryptedPassword = SettingsService.Encrypt(_password.Text ?? string.Empty),
                    Encrypt = _encrypt.Checked ?? true,
                    TrustServerCertificate = _trust.Checked ?? true
                };
                var cs = SettingsService.BuildConnectionString(s);
                // Force TCP provider on legacy client
                if ((cs ?? string.Empty).IndexOf("Network Library=DBMSSOCN", StringComparison.OrdinalIgnoreCase) < 0)
                    cs += ";Network Library=DBMSSOCN";
                Exception last = null;
                // Try Microsoft.Data.SqlClient first
                try
                {
                    using (var conn = new Microsoft.Data.SqlClient.SqlConnection(cs))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT @@VERSION";
                            var ver = (string)cmd.ExecuteScalar();
                            _status.Text = $"OK - {ver.Split('\n')[0].Trim()}";
                            _status.TextColor = Colors.ForestGreen;
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    last = ex;
                }

                // Fallback to System.Data.SqlClient on this platform
                try
                {
                    using (var conn = new System.Data.SqlClient.SqlConnection(cs))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT @@VERSION";
                            var ver = (string)cmd.ExecuteScalar();
                            _status.Text = $"OK (legacy) - {ver.Split('\n')[0].Trim()}";
                            _status.TextColor = Colors.ForestGreen;
                            return;
                        }
                    }
                }
                catch (Exception ex2)
                {
                // ODBC fallback (requires a SQL Server ODBC driver installed)
                foreach (var driver in new[] { "{ODBC Driver 18 for SQL Server}", "{ODBC Driver 17 for SQL Server}", "{SQL Server}" })
                {
                    try
                    {
                        var odbcCs = BuildOdbcConnectionStringForDriver(s, driver);
                        using (var conn = new OdbcConnection(odbcCs))
                        {
                            conn.Open();
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = "SELECT @@VERSION";
                                var ver = (string)cmd.ExecuteScalar();
                                _status.Text = $"OK (ODBC {driver})\n{ver.Split('\n')[0].Trim()}";
                                _status.TextColor = Colors.ForestGreen;
                                return;
                            }
                        }
                    }
                    catch
                    {
                        // try next driver
                    }
                }
                throw new Exception((last?.Message ?? "") + "\nFallback failed: " + ex2.Message + "\nODBC failed: No suitable driver found");
                }
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
                _status.TextColor = Colors.IndianRed;
            }
        }

        private static string BuildOdbcConnectionStringForDriver(DbSettings s, string driver)
        {
            var parts = new System.Collections.Generic.List<string>
            {
                $"Driver={driver}",
                $"Server={s.Server}",
                $"Database={s.Database}",
                $"TrustServerCertificate={(s.TrustServerCertificate ? "Yes" : "No")}"
            };
            if (s.AuthMode == DbAuthMode.Windows)
            {
                parts.Add("Trusted_Connection=Yes");
            }
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


