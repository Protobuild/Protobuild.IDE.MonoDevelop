using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects.Formats.Protobuild;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public abstract class ProtobuildModuleInterfaceNodeBuilder : TypeNodeBuilder
	{
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var protobuildModule = (IProtobuildModule)dataObject;
			nodeInfo.Label = GLib.Markup.EscapeText (protobuildModule.Name);
			nodeInfo.Icon = Context.GetIcon (Stock.Solution);
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject) {
			return ((IProtobuildModule)dataObject).Name;
		}

        public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
        {
            var module = (IProtobuildModule)dataObject;
            if (module.Packages != null) {
                treeBuilder.AddChild (module.Packages);
            }
            foreach (var entry in module.Submodules) {
                treeBuilder.AddChild (entry);
            }
            foreach (var entry in module.Definitions) {
                treeBuilder.AddChild (entry);
            }
        }

        public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
        {
            IProtobuildModule module = (IProtobuildModule)dataObject;
            return module.Packages != null || module.Submodules.Count > 0 || module.Definitions.Count > 0;
        }
    }
}

