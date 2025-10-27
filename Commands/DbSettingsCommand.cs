using Rhino.Commands;
using Rhino.UI;
using RhinoERPBridge.UI;

namespace RhinoERPBridge.Commands
{
    public class DbSettingsCommand : Command
    {
        public override string EnglishName => "DbSettings";

        protected override Result RunCommand(Rhino.RhinoDoc doc, RunMode mode)
        {
            Panels.OpenPanel(DbSettingsPanel.PanelGuid);
            return Result.Success;
        }
    }
}


