using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoDevelop.Projects.Formats.Protobuild
{
    public class ProtobuildSubmodule : IProtobuildModule
    {
        private ProtobuildModuleInfo parentModule;

		private ProtobuildModuleInfo currentModule;

		private SolutionFolder rootFolder;

		public ProtobuildSubmodule (ProtobuildModuleInfo latestModuleInfo, ProtobuildModuleInfo submodule, SolutionFolder rootFolder)
        {
            parentModule = latestModuleInfo;
            currentModule = submodule;
			RootFolder = rootFolder;
        }

        public string Name
        {
            get { return currentModule.Name; }
        }

        public ProtobuildPackages Packages { get; set; }

        public ItemCollection<ProtobuildDefinition> Definitions { get; set; }

        public ItemCollection<ProtobuildSubmodule> Submodules { get; set; }

		public SolutionFolder RootFolder { get; set; }

		public string SupportedPlatformsString
		{
			get
			{
				if (!string.IsNullOrEmpty(currentModule.SupportedPlatforms))
				{
					return currentModule.SupportedPlatforms;
				}

				return ProtobuildModule.DEFAULT_PLATFORMS;
			}
		}

		public string[] SupportedPlatformsArray
		{
			get
			{
				return this.SupportedPlatformsString.Split (',');
			}
		}
    }
}
