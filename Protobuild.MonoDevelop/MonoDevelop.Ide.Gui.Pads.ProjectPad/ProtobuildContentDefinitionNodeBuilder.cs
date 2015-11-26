using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects.Formats.Protobuild;
using MonoDevelop.Core;
using System.Linq;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProtobuildContentDefinitionNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get {
				return typeof(ProtobuildContentDefinition);
			}
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((ProtobuildContentDefinition)dataObject).Name;
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			base.BuildNode (treeBuilder, dataObject, nodeInfo);

			var p = dataObject as ProtobuildContentDefinition;

			string escapedProjectName = GLib.Markup.EscapeText (p.Name);

			nodeInfo.Icon = Context.GetIcon (Stock.Project);
			nodeInfo.Label = escapedProjectName;
		}

		public override void BuildChildNodes(ITreeBuilder builder, object dataObject)
		{
			var definition = (ProtobuildContentDefinition)dataObject;

			foreach (var contentRule in definition.Items.OfType<ProtobuildContentSourceRule>())
			{
				builder.AddChild(contentRule);
			}

			base.BuildChildNodes(builder, dataObject);
		}

		public override bool HasChildNodes(ITreeBuilder builder, object dataObject)
		{
			return true;
		}
	}
}

