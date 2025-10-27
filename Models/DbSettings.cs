namespace RhinoERPBridge.Models
{
    public enum DbAuthMode
    {
        Windows,
        SqlLogin
    }

    public class DbSettings
    {
        public string Server { get; set; } = string.Empty;          // e.g. HOSTNAME or HOST,1433
        public string Database { get; set; } = string.Empty;        // e.g. Standard
        public DbAuthMode AuthMode { get; set; } = DbAuthMode.SqlLogin;
        public string Username { get; set; } = string.Empty;        // ignored for Windows auth
        public string EncryptedPassword { get; set; } = string.Empty; // DPAPI encrypted, base64
        public bool Encrypt { get; set; } = true;                   // SqlClient Encrypt=true
        public bool TrustServerCertificate { get; set; } = true;    // SqlClient TrustServerCertificate

        // Article mapping (table and column names)
        public string ArticlesTable { get; set; } = "dbo.Articles"; // configure to your real table/view
        public string ColSku { get; set; } = "Sku";
        public string ColName { get; set; } = "Name";
        public string ColDescription { get; set; } = "Description";
        public string ColUnit { get; set; } = "Unit";
        public string ColPrice { get; set; } = "Price";
        public string ColStock { get; set; } = "Stock";
        public string ColCategory { get; set; } = "Category";

        // Derived/utility flags
        public bool IsConfigured => !string.IsNullOrWhiteSpace(Server);
    }
}


