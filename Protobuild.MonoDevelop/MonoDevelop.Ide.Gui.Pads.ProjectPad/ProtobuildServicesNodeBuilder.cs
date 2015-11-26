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
    class ProtobuildServicesNodeBuilder : TypeNodeBuilder
    {
        public override Type NodeDataType
        {
            get { return typeof (ProtobuildServices); }
        }

        public override Type CommandHandlerType
        {
            get { return typeof(ProtobuildServicesNodeCommandHandler); }
        }

        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
            nodeInfo.Label = GLib.Markup.EscapeText("Services");
            nodeInfo.Icon = Context.GetIcon(Stock.OpenReferenceFolder);
            nodeInfo.ClosedIcon = Context.GetIcon (Stock.ClosedReferenceFolder);
        }

        public override string GetNodeName(ITreeNavigator thisNode, object dataObject)
        {
            return "Services";
        }

        public override void BuildChildNodes(ITreeBuilder ctx, object dataObject)
        {
            ProtobuildServices services = (ProtobuildServices)dataObject;
            foreach (var entry in services)
            {
                ctx.AddChild(entry);
            }
        }

        public override bool HasChildNodes(ITreeBuilder builder, object dataObject)
        {
            ProtobuildServices services = (ProtobuildServices)dataObject;
            return services.Count > 0;
        }

        public override int CompareObjects(ITreeNavigator thisNode, ITreeNavigator otherNode)
        {
            return -1;
        }
    }

    class ProtobuildServicesNodeCommandHandler : NodeCommandHandler
    {
        
    }
}