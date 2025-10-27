using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rhino.PlugIns;

// Plug-in Description Attributes - all of these are optional.
// These will show in Rhino's option dialog, in the tab Plug-ins.
[assembly: PlugInDescription(DescriptionType.Address, "")]
[assembly: PlugInDescription(DescriptionType.Country, "Switzerland")]
[assembly: PlugInDescription(DescriptionType.Email, "info@example.com")]
[assembly: PlugInDescription(DescriptionType.Phone, "")]
[assembly: PlugInDescription(DescriptionType.Fax, "")]
[assembly: PlugInDescription(DescriptionType.Organization, "Rhino ERP Bridge")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "https://example.com/rhino-erp-bridge/updates")]
[assembly: PlugInDescription(DescriptionType.WebSite, "https://example.com/rhino-erp-bridge")]

// Icons should be Windows .ico files and contain 32-bit images in the following sizes: 16, 24, 32, 48, and 256.
[assembly: PlugInDescription(DescriptionType.Icon, "RhinoERPBridge.EmbeddedResources.plugin-utility.ico")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
// This will also be the Guid of the Rhino plug-in
[assembly: Guid("a33e57d8-f7b9-4699-93c4-8d2cd3c1bbf3")]
