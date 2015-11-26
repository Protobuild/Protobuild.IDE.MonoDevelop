using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects.Formats.Protobuild;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProtobuildSubmoduleNodeBuilder : ProtobuildModuleInterfaceNodeBuilder
	{
		public override Type NodeDataType { get { return typeof(ProtobuildSubmodule); } }
    }
}

