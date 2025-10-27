using System;
using Rhino;
using Rhino.UI;
using RhinoERPBridge.UI;
using System.Linq;
using System.Reflection;
using System.Drawing;

namespace RhinoERPBridge
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class RhinoERPBridgePlugin : Rhino.PlugIns.PlugIn
    {
        public RhinoERPBridgePlugin()
        {
            Instance = this;
        }

        ///<summary>Gets the only instance of the RhinoERPBridgePlugin plug-in.</summary>
        public static RhinoERPBridgePlugin Instance { get; private set; }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and maintain plug-in wide options in a document.

        protected override Rhino.PlugIns.LoadReturnCode OnLoad(ref string errorMessage)
        {
            // Ensure Microsoft.Data.SqlClient works in Rhino by using managed networking on Windows (avoids native SNI load issues)
            RhinoERPBridge.Infrastructure.SqlClientBootstrap.Initialize();
            var icon = LoadEmbeddedIcon();
            Panels.RegisterPanel(this, typeof(ErpSearchPanel), "ERP Search", icon);
            Panels.RegisterPanel(this, typeof(DbSettingsPanel), "DB Settings", icon);
            return Rhino.PlugIns.LoadReturnCode.Success;
        }

        private static Icon LoadEmbeddedIcon()
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("EmbeddedResources.plugin-utility.ico", StringComparison.OrdinalIgnoreCase));
            if (name == null)
                return null;
            using (var stream = asm.GetManifestResourceStream(name))
            {
                return stream != null ? new Icon(stream) : null;
            }
        }
    }
}