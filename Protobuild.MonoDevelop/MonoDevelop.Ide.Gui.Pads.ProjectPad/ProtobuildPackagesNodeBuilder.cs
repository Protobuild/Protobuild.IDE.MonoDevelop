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
    class ProtobuildPackagesNodeBuilder : TypeNodeBuilder
    {
        public override Type NodeDataType
        {
            get { return typeof (ProtobuildPackages); }
        }

        public override Type CommandHandlerType
        {
            get { return typeof(ProtobuildPackagesNodeCommandHandler); }
        }

        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
            nodeInfo.Label = GLib.Markup.EscapeText("Packages");
			nodeInfo.Icon = Context.GetIcon("md-package-source");
			nodeInfo.ClosedIcon = Context.GetIcon ("md-package-source");
        }

        public override string GetNodeName(ITreeNavigator thisNode, object dataObject)
        {
            return "Packages";
        }

        public override void BuildChildNodes(ITreeBuilder ctx, object dataObject)
        {
            ProtobuildPackages packages = (ProtobuildPackages)dataObject;
            foreach (var entry in packages)
            {
                ctx.AddChild(entry);
            }
        }

        public override bool HasChildNodes(ITreeBuilder builder, object dataObject)
        {
            ProtobuildPackages packages = (ProtobuildPackages)dataObject;
            return packages.Count > 0;
        }

        public override int CompareObjects(ITreeNavigator thisNode, ITreeNavigator otherNode)
        {
            return -1;
        }
    }

    class ProtobuildPackagesNodeCommandHandler : NodeCommandHandler
    {
        
    }
}
