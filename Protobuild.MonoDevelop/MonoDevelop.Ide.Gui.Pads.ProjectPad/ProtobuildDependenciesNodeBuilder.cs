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
    class ProtobuildDependenciesNodeBuilder : TypeNodeBuilder
    {
        public override Type NodeDataType
        {
            get { return typeof (ProtobuildDependencies); }
        }

        public override Type CommandHandlerType
        {
            get { return typeof(ProtobuildDependenciesNodeCommandHandler); }
        }

        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
            nodeInfo.Label = GLib.Markup.EscapeText("Dependencies");
            nodeInfo.Icon = Context.GetIcon(Stock.OpenReferenceFolder);
            nodeInfo.ClosedIcon = Context.GetIcon (Stock.ClosedReferenceFolder);
        }

        public override string GetNodeName(ITreeNavigator thisNode, object dataObject)
        {
            return "Dependencies";
        }

        public override void BuildChildNodes(ITreeBuilder ctx, object dataObject)
        {
            ProtobuildDependencies dependencies = (ProtobuildDependencies)dataObject;
            foreach (var entry in dependencies)
            {
                ctx.AddChild(entry);
            }
        }

        public override bool HasChildNodes(ITreeBuilder builder, object dataObject)
        {
            ProtobuildDependencies dependencies = (ProtobuildDependencies)dataObject;
            return dependencies.Count > 0;
        }

        public override int CompareObjects(ITreeNavigator thisNode, ITreeNavigator otherNode)
        {
            return -1;
        }
    }

    class ProtobuildDependenciesNodeCommandHandler : NodeCommandHandler
    {
        
    }
}
