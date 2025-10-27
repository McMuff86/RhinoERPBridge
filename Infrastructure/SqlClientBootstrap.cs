using System;

namespace RhinoERPBridge.Infrastructure
{
    internal static class SqlClientBootstrap
    {
        // invoked from plugin OnLoad to avoid ModuleInitializer on net48
        public static void Initialize()
        {
            try { AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true); } catch { }
            try { AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedSNIOnWindows", true); } catch { }
        }
    }
}


