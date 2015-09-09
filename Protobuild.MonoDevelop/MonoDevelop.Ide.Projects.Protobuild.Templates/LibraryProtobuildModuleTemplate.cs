using MonoDevelop.Protobuild;

namespace MonoDevelop.Ide.Projects.Protobuild.Templates
{
    [BuiltinProtobuildModuleTemplate]
    class LibraryProtobuildModuleTemplate : IProtobuildModuleTemplate
    {
        public string Id
        {
            get { return "commons/Library"; }
        }

        public string Name
        {
            get { return "Single Library"; }
        }

        public string IconId
        {
            get { return "md-project-library"; }
        }

        public string GroupId
        {
            get { return "md-project-library"; }
        }

        public string ImageId
        {
            get { return "md-library-project"; }
        }

        public string Description
        {
            get { return "A module with a single C# library"; }
        }

        public bool IsFeatured
        {
            get { return true; }
        }

        public bool IsGeneral
        {
            get { return true; }
        }

        public bool IsOnline
        {
            get { return false; }
        }

        public bool IsMisc
        {
            get { return false; }
        }
    }
}
