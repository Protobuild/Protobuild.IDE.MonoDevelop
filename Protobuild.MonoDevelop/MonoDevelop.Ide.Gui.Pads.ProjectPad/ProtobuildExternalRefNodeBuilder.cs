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
	class ProtobuildExternalRefNodeBuilder : TypeNodeBuilder
    {
        public override Type NodeDataType
        {
			get { return typeof (ProtobuildExternalRef); }
        }

        public override Type CommandHandlerType
        {
			get { return typeof (ProtobuildExternalRefNodeCommandHandler); }
        }

        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
			var externalRef = (ProtobuildExternalRef) dataObject;

			nodeInfo.Label = GLib.Markup.EscapeText(externalRef.Name ?? externalRef.Path);
			nodeInfo.Icon = Context.GetIcon(Stock.Reference);
        }

        public override string GetNodeName(ITreeNavigator thisNode, object dataObject)
        {
			return ((ProtobuildExternalRef)dataObject).Name ?? ((ProtobuildExternalRef)dataObject).Path;
        }
    }

	class ProtobuildExternalRefNodeCommandHandler : NodeCommandHandler
    {
        
    }
}
