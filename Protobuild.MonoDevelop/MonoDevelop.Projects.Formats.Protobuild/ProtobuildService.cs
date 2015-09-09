using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoDevelop.Projects.Formats.Protobuild
{
    public class ProtobuildService
    {
        public string Name { get; set; }

        public string[] Platforms { get; set; }

        public string[] Conflicts { get; set; }

        public string[] Requires { get; set; }

        public string[] AddDefine { get; set; }

        public string[] RemoveDefine { get; set; }

        public string[] References { get; set; }

        public bool InfersReference { get; set; }
    }
}
