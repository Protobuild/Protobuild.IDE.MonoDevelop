using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects.Formats.Protobuild;
using MonoDevelop.Core;
using System.Linq;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProtobuildExternalDefinitionNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get {
				return typeof(ProtobuildExternalDefinition);
			}
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((ProtobuildDefinition)dataObject).Name;
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			base.BuildNode (treeBuilder, dataObject, nodeInfo);

			var p = dataObject as ProtobuildDefinition;

			string escapedProjectName = GLib.Markup.EscapeText (p.Name);

			nodeInfo.Icon = Context.GetIcon (Stock.Project);
			nodeInfo.Label = escapedProjectName;
		}

		public override void BuildChildNodes(ITreeBuilder builder, object dataObject)
		{
			var definition = (ProtobuildDefinition)dataObject;

			if (definition.References != null) {
				builder.AddChild (definition.References);
			}

			if (definition.Dependencies != null)
			{
				builder.AddChild(definition.Dependencies);
			}

			if (definition.Services != null)
			{
				builder.AddChild(definition.Services);
			}

			foreach (var platformFilter in definition.Items.OfType<ProtobuildPlatformFilter>())
			{
				builder.AddChild(platformFilter);
			}

			foreach (var serviceFilter in definition.Items.OfType<ProtobuildServiceFilter>())
			{
				builder.AddChild(serviceFilter);
			}

			foreach (var externalRef in definition.Items.OfType<ProtobuildExternalRef>())
			{
				builder.AddChild(externalRef);
			}

			base.BuildChildNodes(builder, dataObject);
		}

		public override bool HasChildNodes(ITreeBuilder builder, object dataObject)
		{
			return true;
		}
	}
}

