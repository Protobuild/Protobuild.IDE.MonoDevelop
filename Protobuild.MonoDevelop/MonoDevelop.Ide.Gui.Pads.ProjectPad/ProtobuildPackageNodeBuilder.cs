using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects.Formats.Protobuild;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProtobuildPackageNodeBuilder : ProtobuildModuleInterfaceNodeBuilder
	{
		public override Type NodeDataType { get { return typeof(ProtobuildPackage); } }

		public override void BuildNode(ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			ProtobuildPackage package = (ProtobuildPackage) dataObject;

			nodeInfo.Icon = Context.GetIcon(MonoDevelop.Ide.Gui.Stock.Reference);

			if (package.IsBinary)
			{
				var overlay = ImageService.GetIcon ("md-command").WithSize (Xwt.IconSize.Small);
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
			}

			nodeInfo.Label = GettextCatalog.GetString(
				"{0} <span foreground='grey'><span size='small'>({1}@{2})</span></span>", 
				GLib.Markup.EscapeText(package.Folder),
				GLib.Markup.EscapeText(package.Uri),
				GLib.Markup.EscapeText(package.GitRef));
		}
	}
}

