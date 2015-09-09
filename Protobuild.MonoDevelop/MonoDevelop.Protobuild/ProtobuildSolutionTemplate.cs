using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.Protobuild
{
    public class ProtobuildSolutionTemplate : SolutionTemplate
    {
        public IProtobuildModuleTemplate ModuleTemplate { get; set; }

        public ProtobuildSolutionTemplate (string id, string name, string iconId) : base (id, name, iconId)
        {
        }
    }
}
