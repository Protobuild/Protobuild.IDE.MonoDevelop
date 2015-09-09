using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects.Formats.Protobuild;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
    class ProtobuildSubmoduleNodeBuilder : ProtobuildModuleInterfaceNodeBuilder
    {
        public override Type NodeDataType
        {
            get { return typeof (ProtobuildSubmodule); }
        }

        public override Type CommandHandlerType
        {
            get { return typeof(ProtobuildSubmoduleNodeCommandHandler); }
        }

        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
            ProtobuildSubmodule submodule = (ProtobuildSubmodule)dataObject;

            nodeInfo.Label = GLib.Markup.EscapeText(submodule.Name);
            nodeInfo.Icon = Context.GetIcon(Stock.Solution);
        }

        public override string GetNodeName(ITreeNavigator thisNode, object dataObject)
        {
            return ((ProtobuildSubmodule)dataObject).Name;
        }
    }

    class ProtobuildSubmoduleNodeCommandHandler : NodeCommandHandler
    {
        
    }
}
