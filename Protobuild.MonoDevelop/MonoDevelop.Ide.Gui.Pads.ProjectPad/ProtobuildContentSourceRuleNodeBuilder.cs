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
	class ProtobuildContentSourceRuleNodeBuilder : TypeNodeBuilder
    {
        public override Type NodeDataType
        {
			get { return typeof (ProtobuildContentSourceRule); }
        }

        public override Type CommandHandlerType
        {
			get { return typeof (ProtobuildContentSourceRuleNodeCommandHandler); }
        }

        public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
			ProtobuildContentSourceRule rule = (ProtobuildContentSourceRule) dataObject;

			nodeInfo.Icon = Context.GetIcon(Stock.OpenReferenceFolder);
			nodeInfo.ClosedIcon = Context.GetIcon(Stock.ClosedReferenceFolder);

			if (rule.Primary)
			{
				var overlay = ImageService.GetIcon ("md-done").WithSize (Xwt.IconSize.Small);
				var cached = Context.GetComposedIcon (nodeInfo.Icon, overlay);
				if (cached != null)
					nodeInfo.Icon = cached;
				else {
					var ib = new Xwt.Drawing.ImageBuilder (nodeInfo.Icon.Width, nodeInfo.Icon.Height);
					ib.Context.DrawImage (nodeInfo.Icon, 0, 0);
					ib.Context.DrawImage (overlay, 8, 8, 8, 8);
					var res = ib.ToVectorImage ();
					ib.Dispose ();
					Context.CacheComposedIcon (nodeInfo.Icon, overlay, res);
					nodeInfo.Icon = res;
				}

				cached = Context.GetComposedIcon (nodeInfo.ClosedIcon, overlay);
				if (cached != null)
					nodeInfo.ClosedIcon = cached;
				else {
					var ib = new Xwt.Drawing.ImageBuilder (nodeInfo.ClosedIcon.Width, nodeInfo.ClosedIcon.Height);
					ib.Context.DrawImage (nodeInfo.ClosedIcon, 0, 0);
					ib.Context.DrawImage (overlay, 8, 8, 8, 8);
					var res = ib.ToVectorImage ();
					ib.Dispose ();
					Context.CacheComposedIcon (nodeInfo.ClosedIcon, overlay, res);
					nodeInfo.ClosedIcon = res;
				}
			}

            nodeInfo.Label = GettextCatalog.GetString(
                "{0} <span foreground='grey'><span size='small'>(files matching {1})</span></span>", 
				GLib.Markup.EscapeText(rule.Include),
				GLib.Markup.EscapeText(rule.Match));
        }

        public override string GetNodeName(ITreeNavigator thisNode, object dataObject)
        {
			return ((ProtobuildContentSourceRule)dataObject).Include;
        }
    }

	class ProtobuildContentSourceRuleNodeCommandHandler : NodeCommandHandler
    {
        
    }
}