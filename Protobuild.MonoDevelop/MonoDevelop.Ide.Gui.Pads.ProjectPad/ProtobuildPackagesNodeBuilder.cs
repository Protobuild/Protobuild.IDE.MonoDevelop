using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects.Formats.Protobuild;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProtobuildPackagesNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType { get { return typeof(ProtobuildPackages); } }

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject) {
			return "Packages";
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			nodeInfo.Label = GLib.Markup.EscapeText("Packages");
			nodeInfo.Icon = Context.GetIcon("md-package-source");
			nodeInfo.ClosedIcon = Context.GetIcon ("md-package-source");
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var packages = (ProtobuildPackages)dataObject;
			foreach (var entry in packages)
			{
				treeBuilder.AddChild(entry);
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var packages = (ProtobuildPackages)dataObject;
			return packages.Count > 0;
		}

		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			return -1;
		}
	}
}

