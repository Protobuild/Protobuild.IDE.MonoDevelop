using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects.Formats.Protobuild;

#if MONODEVELOP_5

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
    class ProtobuildReferencesNodeBuilder : TypeNodeBuilder
    {
        public override Type NodeDataType
        {
            get { return typeof (ProtobuildReferences); }
        }

        public override Type CommandHandlerType
        {
            get { return typeof(ProtobuildReferencesNodeCommandHandler); }
        }

        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
            nodeInfo.Label = GLib.Markup.EscapeText("References");
            nodeInfo.Icon = Context.GetIcon(Stock.OpenReferenceFolder);
            nodeInfo.ClosedIcon = Context.GetIcon (Stock.ClosedReferenceFolder);
        }

        public override string GetNodeName(ITreeNavigator thisNode, object dataObject)
        {
            return "References";
        }

        public override void BuildChildNodes(ITreeBuilder ctx, object dataObject)
        {
            ProtobuildReferences references = (ProtobuildReferences)dataObject;
            foreach (var entry in references)
            {
                ctx.AddChild(entry);
            }
        }

        public override bool HasChildNodes(ITreeBuilder builder, object dataObject)
        {
            ProtobuildReferences references = (ProtobuildReferences)dataObject;
            return references.Count > 0;
        }

        public override int CompareObjects(ITreeNavigator thisNode, ITreeNavigator otherNode)
        {
            return -1;
        }
    }

    class ProtobuildReferencesNodeCommandHandler : NodeCommandHandler
    {
        
    }
}

#endif