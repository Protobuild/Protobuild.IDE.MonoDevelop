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
    class ProtobuildModuleNodeBuilder : SolutionNodeBuilder
    {
        public override Type NodeDataType
        {
            get { return typeof (ProtobuildModule); }
        }

        public override Type CommandHandlerType
        {
            get { return typeof (ProtobuildModuleNodeCommandHandler); }
        }

        public override void BuildChildNodes(ITreeBuilder ctx, object dataObject)
        {
            IProtobuildModule module = (IProtobuildModule)dataObject;
            if (module.Packages != null) {
                ctx.AddChild (module.Packages);
            }
            foreach (var entry in module.Submodules) {
                ctx.AddChild (entry);
            }
            foreach (var entry in module.Definitions) {
                ctx.AddChild (entry);
            }
        }

        public override bool HasChildNodes(ITreeBuilder builder, object dataObject)
        {
            IProtobuildModule module = (IProtobuildModule)dataObject;
            return module.Packages != null || module.Submodules.Count > 0 || module.Definitions.Count > 0;
        }
    }

    class ProtobuildModuleNodeCommandHandler : NodeCommandHandler
    {
        
    }
}
