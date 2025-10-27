using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using RhinoERPBridge.Models;

namespace RhinoERPBridge.Services
{
    public static class SettingsService
    {
        private static string SettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RhinoERPBridge");
        private static string SettingsFile => Path.Combine(SettingsDirectory, "settings.json");

        public static DbSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile, Encoding.UTF8);
                    var s = JsonConvert.DeserializeObject<DbSettings>(json);
                    if (s != null) return s;
                }
            }
            catch { /* ignore and return defaults */ }
            return new DbSettings();
        }

        public static void Save(DbSettings s)
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonConvert.SerializeObject(s, Formatting.Indented);
            File.WriteAllText(SettingsFile, json, Encoding.UTF8);
        }

        public static string Encrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return string.Empty;
            var data = Encoding.UTF8.GetBytes(plain);
            var enc = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(enc);
        }

        public static string Decrypt(string cipher)
        {
            if (string.IsNullOrEmpty(cipher)) return string.Empty;
            var enc = Convert.FromBase64String(cipher);
            var dec = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(dec);
        }

        public static string BuildConnectionString(DbSettings s)
        {
            if (s.AuthMode == DbAuthMode.Windows)
            {
                return $"Server={s.Server};Database={s.Database};Trusted_Connection=True;Encrypt={(s.Encrypt ? "True" : "False")};TrustServerCertificate={(s.TrustServerCertificate ? "True" : "False")};";
            }
            var pwd = Decrypt(s.EncryptedPassword);
            // Force managed networking; disable pooling for predictability while diagnosing
            return $"Server={s.Server};Database={s.Database};User Id={s.Username};Password={pwd};Encrypt={(s.Encrypt ? "True" : "False")};TrustServerCertificate={(s.TrustServerCertificate ? "True" : "False")};Pooling=False;MultipleActiveResultSets=False;Application Name=RhinoERPBridge;";
        }
    }
}


