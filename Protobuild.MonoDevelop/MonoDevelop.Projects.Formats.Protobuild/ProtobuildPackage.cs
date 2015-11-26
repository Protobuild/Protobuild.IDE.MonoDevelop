using System;
using System.IO;

namespace MonoDevelop.Projects.Formats.Protobuild
{
    public class ProtobuildPackage : WorkspaceObject, IProtobuildModule
    {
        private ProtobuildModuleInfo module;

        private ProtobuildPackageRef package;

		private IProtobuildModule parentModuleRef;

		public ProtobuildPackage (ProtobuildModuleInfo moduleInfo, ProtobuildPackageRef reference, IProtobuildModule parentModule)
        {
            module = moduleInfo;
            package = reference;
			parentModuleRef = parentModule;
			Packages = new ProtobuildPackages(this);
			Definitions = new ItemCollection<ProtobuildDefinition>();
			Submodules = new ItemCollection<ProtobuildSubmodule>();

			IsBinary = File.Exists(Path.Combine(FullPath, ".pkg")) &&
				!File.Exists(Path.Combine(FullPath, ".git")) &&
				!Directory.Exists(Path.Combine(FullPath, ".git"));
        }

        public string Folder
        {
            get { return package.Folder; }
        }

		string IProtobuildModule.Name {
			get { return Folder; }
		}

        public string Uri { get { return package.Uri; } }

        public string GitRef { get { return package.GitRef; } }

		public bool IsBinary { get; private set; }

        public string FullPath
        {
            get { return Path.Combine (module.Path, package.Folder); }
        }

        public ProtobuildPackages Packages { get; set; }

        public ItemCollection<ProtobuildDefinition> Definitions { get; set; }

        public ItemCollection<ProtobuildSubmodule> Submodules { get; set; }

		public SolutionFolder RootFolder { get { return parentModuleRef.RootFolder; } set { parentModuleRef.RootFolder = value; } }

		public string[] SupportedPlatformsArray { get { return parentModuleRef.SupportedPlatformsArray; } }

        protected override string OnGetName ()
        {
			return Folder;
        }

        protected override string OnGetItemDirectory ()
        {
			return module.Path;
        }

        protected override string OnGetBaseDirectory ()
		{
			return module.Path;
        }
    }
}
