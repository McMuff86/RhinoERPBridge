using System;
using System.Runtime.InteropServices;
using Eto.Forms;
using Eto.Drawing;
using Microsoft.Data.SqlClient;
using System.Data.SqlClient; // legacy fallback
using System.Data.Odbc; // ODBC fallback
using RhinoERPBridge.Models;
using RhinoERPBridge.Services;

namespace RhinoERPBridge.UI
{
    [Guid("0C4C1FE3-2E3E-4A9D-AE10-8C35B0E6F2A9")]
    public class DsnSettingsPanel : Panel
    {
        public static Guid PanelGuid => typeof(DsnSettingsPanel).GUID;

        private readonly TextBox _server = new TextBox();
        private readonly TextBox _database = new TextBox();
        private readonly DropDown _auth = new DropDown { DataStore = new[] { "Windows", "SQL Login" } };
        private readonly TextBox _username = new TextBox();
        private readonly PasswordBox _password = new PasswordBox();
        private readonly CheckBox _encrypt = new CheckBox { Text = "Encrypt" };
        private readonly CheckBox _trust = new CheckBox { Text = "Trust Server Certificate" };
        private readonly Label _status = new Label { TextColor = Colors.Gray, Wrap = WrapMode.Word };

        public DsnSettingsPanel()
        {
            var s = SettingsService.Load();
            _server.Text = s.Server;
            _database.Text = s.Database;
            _auth.SelectedIndex = s.AuthMode == DbAuthMode.Windows ? 0 : 1;
            _username.Text = s.Username;
            _password.Text = SettingsService.Decrypt(s.EncryptedPassword);
            _encrypt.Checked = s.Encrypt;
            _trust.Checked = s.TrustServerCertificate;

            _auth.SelectedIndexChanged += (s2, e2) => UpdateAuthVisibility();
            UpdateAuthVisibility();

            var btnTest = new Button { Text = "Test Connection" };
            var btnSave = new Button { Text = "Save" };

            btnTest.Click += (s2, e2) => Test();
            btnSave.Click += (s2, e2) => Save();

            var layout = new DynamicLayout { Spacing = new Size(6, 6), Padding = new Padding(8) };
            layout.AddRow(new Label { Text = "Server" }, _server);
            layout.AddRow(new Label { Text = "Database" }, _database);
            layout.AddRow(new Label { Text = "Authentication" }, _auth);
            layout.AddRow(new Label { Text = "Username" }, _username);
            layout.AddRow(new Label { Text = "Password" }, _password);
            layout.AddRow(_encrypt);
            layout.AddRow(_trust);
            layout.AddRow(btnTest, btnSave);
            layout.AddRow(_status);
            Content = layout;
        }

        private void UpdateAuthVisibility()
        {
            var isWindows = _auth.SelectedIndex == 0;
            _username.Enabled = !isWindows;
            _password.Enabled = !isWindows;
        }

        private void Save()
        {
            var s = SettingsService.Load();
            s.Server = _server.Text?.Trim() ?? string.Empty;
            s.Database = _database.Text?.Trim() ?? string.Empty;
            s.AuthMode = _auth.SelectedIndex == 0 ? DbAuthMode.Windows : DbAuthMode.SqlLogin;
            s.Username = _username.Text?.Trim() ?? string.Empty;
            s.EncryptedPassword = SettingsService.Encrypt(_password.Text ?? string.Empty);
            s.Encrypt = _encrypt.Checked ?? true;
            s.TrustServerCertificate = _trust.Checked ?? true;
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
                if ((cs ?? string.Empty).IndexOf("Network Library=DBMSSOCN", StringComparison.OrdinalIgnoreCase) < 0)
                    cs += ";Network Library=DBMSSOCN";
                Exception last = null;
                try
                {
                    using (var conn = new Microsoft.Data.SqlClient.SqlConnection(cs))
                    { conn.Open(); }
                    _status.Text = "Connection OK (SqlClient)";
                    _status.TextColor = Colors.ForestGreen;
                    return;
                }
                catch (Exception ex) { last = ex; }
                try
                {
                    using (var conn = new System.Data.SqlClient.SqlConnection(cs))
                    { conn.Open(); }
                    _status.Text = "Connection OK (legacy)";
                    _status.TextColor = Colors.ForestGreen;
                    return;
                }
                catch (Exception ex2)
                {
                    foreach (var driver in new[] { "{ODBC Driver 18 for SQL Server}", "{ODBC Driver 17 for SQL Server}", "{SQL Server}" })
                    {
                        try
                        {
                            var odbcCs = BuildOdbcConnectionStringForDriver(s, driver);
                            using (var conn = new OdbcConnection(odbcCs))
                            { conn.Open(); }
                            _status.Text = $"Connection OK (ODBC {driver})";
                            _status.TextColor = Colors.ForestGreen;
                            return;
                        }
                        catch { }
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


