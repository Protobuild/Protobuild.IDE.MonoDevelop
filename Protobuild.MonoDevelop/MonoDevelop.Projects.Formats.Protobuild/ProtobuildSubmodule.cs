using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoDevelop.Projects.Formats.Protobuild
{
    public class ProtobuildSubmodule : WorkspaceObject, IProtobuildModule
    {
        private ProtobuildModuleInfo parentModule;

		private ProtobuildModuleInfo currentModule;

		private SolutionFolder rootFolder;

		public ProtobuildSubmodule (ProtobuildModuleInfo latestModuleInfo, ProtobuildModuleInfo submodule, SolutionFolder rootFolder)
        {
            parentModule = latestModuleInfo;
            currentModule = submodule;
			RootFolder = rootFolder;
			Packages = new ProtobuildPackages(this);
			Definitions = new ItemCollection<IProtobuildDefinition>();
			Submodules = new ItemCollection<ProtobuildSubmodule>();
		}

        public ProtobuildPackages Packages { get; set; }

        public ItemCollection<IProtobuildDefinition> Definitions { get; set; }

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

        protected override string OnGetName ()
        {
			return currentModule.Name; 
        }

        protected override string OnGetItemDirectory ()
		{
			return currentModule.Path;
        }

        protected override string OnGetBaseDirectory ()
		{
			return currentModule.Path;
        }
    }
}
