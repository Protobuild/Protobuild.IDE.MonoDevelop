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
    class ProtobuildReferenceNodeBuilder : TypeNodeBuilder
    {
        public override Type NodeDataType
        {
            get { return typeof (ProtobuildReference); }
        }

        public override Type CommandHandlerType
        {
            get { return typeof (ProtobuildReferenceNodeCommandHandler); }
        }

        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
            ProtobuildReference reference = (ProtobuildReference)dataObject;

            nodeInfo.Icon = Context.GetIcon(MonoDevelop.Ide.Gui.Stock.Reference);
            nodeInfo.Label = GLib.Markup.EscapeText (reference.Name);
        }

        public override string GetNodeName(ITreeNavigator thisNode, object dataObject)
        {
            return ((ProtobuildReference)dataObject).Name;
        }
    }

    class ProtobuildReferenceNodeCommandHandler : NodeCommandHandler
    {
        
    }
}
