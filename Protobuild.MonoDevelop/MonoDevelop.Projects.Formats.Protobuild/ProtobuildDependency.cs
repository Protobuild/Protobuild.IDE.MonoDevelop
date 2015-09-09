using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoDevelop.Projects.Formats.Protobuild
{
    public class ProtobuildDependency
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string[] Platforms { get; set; }
    }
}
