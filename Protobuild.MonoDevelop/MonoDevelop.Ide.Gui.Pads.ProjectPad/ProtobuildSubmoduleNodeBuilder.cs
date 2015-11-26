using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects.Formats.Protobuild;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProtobuildSubmoduleNodeBuilder : ProtobuildModuleInterfaceNodeBuilder
	{
		public override Type NodeDataType { get { return typeof(ProtobuildSubmodule); } }

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject) {
			return "\x01" + ((IProtobuildModule)dataObject).Name;
		}
    }
}

