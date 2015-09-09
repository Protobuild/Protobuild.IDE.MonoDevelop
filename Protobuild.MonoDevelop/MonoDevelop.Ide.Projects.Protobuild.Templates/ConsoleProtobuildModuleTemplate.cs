using MonoDevelop.Protobuild;

namespace MonoDevelop.Ide.Projects.Protobuild.Templates
{
    [BuiltinProtobuildModuleTemplate]
    class ConsoleProtobuildModuleTemplate : IProtobuildModuleTemplate
    {
        public string Id
        {
            get { return "commons/Console"; }
        }

        public string Name
        {
            get { return "Console Application"; }
        }

        public string IconId
        {
            get { return "md-project-console"; }
        }

        public string GroupId
        {
            get { return "md-project-console"; }
        }

        public string ImageId
        {
            get { return "md-console-project"; }
        }

        public string Description
        {
            get { return "A module with a single C# console application"; }
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
