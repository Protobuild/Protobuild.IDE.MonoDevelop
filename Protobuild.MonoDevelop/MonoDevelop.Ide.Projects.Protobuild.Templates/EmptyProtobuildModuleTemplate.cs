using MonoDevelop.Protobuild;

namespace MonoDevelop.Ide.Projects.Protobuild.Templates
{
    [BuiltinProtobuildModuleTemplate]
    class EmptyProtobuildModuleTemplate : IProtobuildModuleTemplate
    {
        public string Id
        {
            get { return "commons/Empty"; }
        }

        public string Name
        {
            get { return "Empty Module"; }
        }

        public string IconId
        {
            get { return "md-project"; }
        }

        public string ImageId
        {
            get { return "md-project-empty"; }
        }

        public string GroupId
        {
            get { return "md-empty-project"; }
        }

        public string Description
        {
            get { return "A module with no projects"; }
        }

        public bool IsFeatured
        {
            get { return false; }
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
            get { return true; }
        }
    }
}
