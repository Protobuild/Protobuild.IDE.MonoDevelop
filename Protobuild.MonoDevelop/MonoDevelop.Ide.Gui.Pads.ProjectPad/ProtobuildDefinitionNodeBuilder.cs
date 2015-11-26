using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects.Formats.Protobuild;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProtobuildDefinitionNodeBuilder : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProtobuildDefinition).IsAssignableFrom(dataType);
		}

		protected override void Initialize ()
		{
			base.Initialize ();

			IdeApp.Workspace.ActiveConfigurationChanged += IdeAppWorkspaceActiveConfigurationChanged;
		}

		public override void Dispose ()
		{
			base.Dispose ();

			IdeApp.Workspace.ActiveConfigurationChanged -= IdeAppWorkspaceActiveConfigurationChanged;
		}

		void IdeAppWorkspaceActiveConfigurationChanged (object sender, EventArgs e)
		{
			foreach (var p in IdeApp.Workspace.GetAllProjects ().OfType<ProtobuildDefinition>()) {
				ITreeBuilder tb = Context.GetTreeBuilder (p);
				if (tb != null)
					tb.UpdateAll ();
			}
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

			foreach (var contentRule in definition.Items.OfType<ProtobuildContentSourceRule>())
			{
				builder.AddChild(contentRule);
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

