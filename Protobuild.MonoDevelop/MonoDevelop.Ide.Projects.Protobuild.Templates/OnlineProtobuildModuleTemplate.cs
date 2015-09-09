using MonoDevelop.Protobuild;

namespace MonoDevelop.Ide.Projects.Protobuild.Templates
{
    class OnlineProtobuildModuleTemplate : IProtobuildModuleTemplate
    {
        public OnlineProtobuildModuleTemplate (string id, string desc)
        {
            Id = id;
            Description = desc;
        }

        public string Id { get; set; }

        public string Name
        {
            get { return Id; }
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
            get { return Id; }
        }

        public string Description { get; set; }

        public bool IsFeatured
        {
            get { return false; }
        }

        public bool IsGeneral
        {
            get { return false; }
        }

        public bool IsOnline
        {
            get { return true; }
        }

        public bool IsMisc
        {
            get { return false; }
        }
    }
}
