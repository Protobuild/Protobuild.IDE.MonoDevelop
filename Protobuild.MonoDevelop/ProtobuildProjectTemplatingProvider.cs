using MonoDevelop.Ide.Templates;
using System.Collections.Generic;
using System;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects;
using System.IO;
using System.Web.Script.Serialization;
using System.Diagnostics;
using System.Text;
using System.Linq;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Projects.Formats.MSBuild;
using MonoDevelop.Projects.Extensions;

namespace Protobuild.MonoDevelop
{
    public class ProtobuildProjectTemplatingProvider : IProjectTemplatingProvider
    {
        public ProtobuildProjectTemplatingProvider ()
        {
        }

        public IEnumerable<SolutionTemplate> GetTemplates ()
        {
            var dir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".xamarin-templates"));
            var serializer = new JavaScriptSerializer();
            var templates = new List<SolutionTemplate>();

            foreach (var file in dir.GetFiles("*.tpl"))
            {
                string templateId;
                string templateName;
                string templateCategory;
                string protobuildManagerExecutablePath;
                string protobuildManagerWorkingDirectory;
                string protobuildManagerTemplateURL;

                using (var reader = new StreamReader(file.FullName))
                {
                    var dict = serializer.Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
                    templateId = (string)dict["TemplateId"];
                    templateName = (string)dict["TemplateName"];
                    templateCategory = (string)dict["TemplateCategory"];
                    protobuildManagerExecutablePath = (string)dict["ProtobuildManagerExecutablePath"];
                    protobuildManagerWorkingDirectory = (string)dict["ProtobuildManagerWorkingDirectory"];
                    protobuildManagerTemplateURL = (string)dict["ProtobuildManagerTemplateURL"];
                }

                var template = new SolutionTemplate(templateId, templateName, "md-project-gui");
                template.Category = "crossplat/" + templateCategory + "/general";
                template.DefaultParameters = "ProtobuildManagerExecutablePath=" + protobuildManagerExecutablePath + ";" +
                    "ProtobuildManagerWorkingDirectory=" + protobuildManagerWorkingDirectory + ";" +
                    "ProtobuildManagerTemplateURL=" + protobuildManagerTemplateURL;
                templates.Add(template);
            }

            return templates;
        }

        public bool CanProcessTemplate (SolutionTemplate template)
        {
            return true;
        }

        public ProcessedTemplateResult ProcessTemplate (SolutionTemplate template, NewProjectConfiguration config, SolutionFolder parentFolder)
        {
            var parameters = template.DefaultParameters.Split(';').Select(x => x.Trim().Split(new[] {'='}, 2)).ToDictionary(kv => kv[0], kv => kv[1]);

            var protobuildManager = parameters["ProtobuildManagerExecutablePath"];
            var workingDirectory = parameters["ProtobuildManagerWorkingDirectory"];
            var templateUrl = parameters["ProtobuildManagerTemplateURL"];

            var projectName = config.SolutionName;
            var solutionDirectory = config.SolutionLocation;

            if (Directory.Exists(solutionDirectory))
            {
                // We don't want this directory.
                Directory.Delete(solutionDirectory, true);
            }

            var serializer = new JavaScriptSerializer();
            var data = serializer.Serialize(new
                {
                    templateurl = templateUrl,
                    prefilledprojectname = projectName,
                    prefilleddestinationdirectory = solutionDirectory,
                    monodeveloppid = Process.GetCurrentProcess().Id,
                });

            var encodedState = Convert.ToBase64String(Encoding.ASCII.GetBytes(data));

            var processStartInfo = new ProcessStartInfo(protobuildManager,
                "ProjectNamingWorkflow " + encodedState)
            {
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
            };
            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Can't start the Protobuild manager!");
            }

            while (!process.HasExited)
            {
                if (!Directory.Exists(solutionDirectory))
                {
                    System.Threading.Thread.Sleep(100);
                    continue;
                }

                if (!File.Exists(Path.Combine(solutionDirectory, "Protobuild.exe")) &&
                    new DirectoryInfo(solutionDirectory).GetFiles("*.sln").Length == 0)
                {
                    System.Threading.Thread.Sleep(100);
                }
                else
                {
                    break;
                }
            }           

            var hostPlatform = Path.DirectorySeparatorChar == '/' ? (Directory.Exists("/Library") ? "MacOS" : "Linux") : "Windows";
            var solutionFile = Path.Combine(solutionDirectory, projectName + "." + hostPlatform + ".sln");

            while (!File.Exists(solutionFile) && !process.HasExited)
            {
                System.Threading.Thread.Sleep(100);
            }

            IdeApp.Workspace.OpenWorkspaceItem(new FilePath(solutionFile));

            return new DefaultSolutionProcessedTemplateResult(solutionFile);
        }

        private class DefaultSolutionProcessedTemplateResult : ProcessedTemplateResult
        {
            public DefaultSolutionProcessedTemplateResult(string solutionFile)
            {
                this.SolutionFileName = solutionFile;
                //this.ProjectBasePath = new FileInfo(solutionFile).DirectoryName;
            }

            public override bool HasPackages ()
            {
                return false;
            }

            public override IEnumerable<IWorkspaceFileObject> WorkspaceItems {
                get {
                    return new IWorkspaceFileObject[]
                    {
                        new DummyWorkspaceItem()
                    };
                }
            }

            public override IEnumerable<string> Actions {
                get {
                    return new string[0];//[] { this.SolutionFileName };
                }
            }

            public override IList<PackageReferencesForCreatedProject> PackageReferences {
                get {
                    return new PackageReferencesForCreatedProject[0];
                }
            }
        }

        private class DummyWorkspaceItem : IWorkspaceFileObject
        {
            public void ConvertToFormat (FileFormat format, bool convertChildren)
            {
            }

            public bool SupportsFormat (FileFormat format)
            {
                return true;
            }

            public List<FilePath> GetItemFiles (bool includeReferencedFiles)
            {
                return new List<FilePath>();
            }

            private class IntFileFormat : IFileFormat
            {
                public FilePath GetValidFormatName (object obj, FilePath fileName)
                {
                    return new FilePath("/");
                }
                public bool CanReadFile (FilePath file, Type expectedObjectType)
                {
                    return true;
                }
                public bool CanWriteFile (object obj)
                {
                    return true;
                }
                public void ConvertToFormat (object obj)
                {
                }
                public void WriteFile (FilePath file, object obj, IProgressMonitor monitor)
                {
                }
                public object ReadFile (FilePath file, Type expectedType, IProgressMonitor monitor)
                {
                    return new object();
                }
                public List<FilePath> GetItemFiles (object obj)
                {
                    return new List<FilePath>();
                }
                public IEnumerable<string> GetCompatibilityWarnings (object obj)
                {
                    return new List<string>();
                }
                public bool SupportsFramework (global::MonoDevelop.Core.Assemblies.TargetFramework framework)
                {
                    return true;
                }
                public bool SupportsMixedFormats {
                    get {
                        return false;
                    }
                }
            }

            public FileFormat FileFormat {
                get {
                    var fileFormatConstructor = typeof(FileFormat).GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).First(x => x.GetParameters().Length == 3);
                    return (FileFormat)fileFormatConstructor.Invoke(new object[] { new IntFileFormat(), "protobuild", "WhyDoesMonoDevelopNeedThisSillyWorkaround" });
                }
            }

            public FilePath FileName {
                get {
                    return new FilePath("/tmp/tmpfile");
                }
                set {
                }
            }
            public bool NeedsReload {
                get {
                    return true;
                }
                set {
                    
                }
            }
            public bool ItemFilesChanged {
                get {
                    return false;
                }
            }

            public void Save (IProgressMonitor monitor)
            {
            }
            public string Name {
                get {
                    return "temp";
                }
                set {
                    
                }
            }
            public FilePath ItemDirectory {
                get {
                    return new FilePath("/");
                }
            }
            public FilePath BaseDirectory {
                get {
                    return new FilePath("/tmp");
                }
                set {
                    
                }
            }

            public void Dispose ()
            {
                
            }

            public System.Collections.IDictionary ExtendedProperties {
                get {
                    return new Dictionary<string, string>();
                }
            }
        }
    }
}

