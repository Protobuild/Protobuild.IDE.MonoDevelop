using MonoDevelop.Projects;

namespace MonoDevelop.Projects.Formats.Protobuild
{
    public class ProtobuildProjectFile : ProjectFile
    {
        public string[] IncludePlatforms { get; set; }

        public string[] ExcludePlatforms { get; set; }

        public string[] IncludeServices { get; set; }

        public string[] ExcludeServices { get; set; }
    }
}
