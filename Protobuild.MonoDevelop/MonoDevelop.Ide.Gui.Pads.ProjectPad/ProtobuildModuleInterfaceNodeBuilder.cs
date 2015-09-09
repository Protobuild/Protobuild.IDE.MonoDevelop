using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects.Formats.Protobuild;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
    abstract class ProtobuildModuleInterfaceNodeBuilder : TypeNodeBuilder
    {
        public override void BuildChildNodes(ITreeBuilder ctx, object dataObject)
        {
            IProtobuildModule module = (IProtobuildModule)dataObject;
            if (module.Packages != null)
            {
                ctx.AddChild(module.Packages);
            }
            foreach (var entry in module.Submodules)
            {
                ctx.AddChild(entry);
            }
            foreach (var entry in module.Definitions)
            {
                ctx.AddChild(entry);
            }
        }

        public override bool HasChildNodes(ITreeBuilder builder, object dataObject)
        {
            IProtobuildModule module = (IProtobuildModule)dataObject;
            return module.Packages != null || (module.Submodules != null && module.Submodules.Count > 0) ||
                   (module.Definitions != null && module.Definitions.Count > 0);
        }
    }
}
