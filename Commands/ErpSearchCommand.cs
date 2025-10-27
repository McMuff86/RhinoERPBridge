using System;
using Rhino.Commands;
using Rhino.UI;
using RhinoERPBridge.UI;

namespace RhinoERPBridge.Commands
{
    public class ErpSearchCommand : Command
    {
        public static ErpSearchCommand Instance { get; private set; }

        public ErpSearchCommand()
        {
            Instance = this;
        }

        public override string EnglishName => "ErpSearch";

        protected override Result RunCommand(Rhino.RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            Panels.OpenPanel(ErpSearchPanel.PanelGuid);
            return Result.Success;
        }
    }
}


