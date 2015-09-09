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
    class ProtobuildServiceFilterNodeBuilder : TypeNodeBuilder
    {
        public override Type NodeDataType
        {
			get { return typeof (ProtobuildServiceFilter); }
        }

        public override Type CommandHandlerType
        {
			get { return typeof(ProtobuildServiceFilterNodeCommandHandler); }
        }

        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
			var filter = (ProtobuildServiceFilter)dataObject;

			nodeInfo.Label = GLib.Markup.EscapeText(filter.Name);
			nodeInfo.Icon = Context.GetIcon(Stock.OpenFolder);
			nodeInfo.ClosedIcon = Context.GetIcon (Stock.ClosedFolder);
        }

        public override string GetNodeName(ITreeNavigator thisNode, object dataObject)
        {
			return ((ProtobuildServiceFilter)dataObject).Name;
        }

        public override void BuildChildNodes(ITreeBuilder ctx, object dataObject)
        {
			var filter = (ProtobuildServiceFilter)dataObject;
			foreach (var entry in filter.Items)
            {
                ctx.AddChild(entry);
            }
        }

        public override bool HasChildNodes(ITreeBuilder builder, object dataObject)
        {
			var filter = (ProtobuildServiceFilter)dataObject;
			return filter.Items.Count > 0;
        }

        public override int CompareObjects(ITreeNavigator thisNode, ITreeNavigator otherNode)
        {
            return -1;
        }
    }

	class ProtobuildServiceFilterNodeCommandHandler : NodeCommandHandler
    {
        
    }
}
