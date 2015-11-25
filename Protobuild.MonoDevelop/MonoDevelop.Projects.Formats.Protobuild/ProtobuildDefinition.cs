using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects.Formats.Protobuild
{
    public class ProtobuildDefinition : Project
    {
        private readonly ProtobuildModuleInfo module;

        private readonly ProtobuildDefinitionInfo definition;

		public ProtobuildDefinition (ProtobuildModuleInfo modulel, ProtobuildDefinitionInfo definitionl, IProtobuildModule moduleObj)
        {
            module = modulel;
            definition = definitionl;

            Initialize(this);

            Configurations.Clear ();

			var document = new XmlDocument ();
			document.Load(definition.DefinitionPath);

            switch (definition.Type) {
				case "External":
					ImportExternalProject(document, moduleObj);
					break;
				case "Content":
					ImportContentProject(document, moduleObj);
                    break;
				default:
					ImportStandardProject(document, moduleObj);
                    break;
            }

            base.Name = definition.Name;
        }



		#region Standard projects

		private void ImportStandardProject(XmlDocument document, IProtobuildModule moduleObj)
		{
			var specificPlatforms = false;

			if (document.DocumentElement != null) {
				if (document.DocumentElement.HasAttribute ("Platforms")) {
					specificPlatforms = true;
					var platforms = document.DocumentElement.Attributes["Platforms"].Value.Split (',');
					foreach (var platform in platforms) {
						AddNewConfiguration (platform);
					}
				}
			}

			if (!specificPlatforms) {
				foreach (var platform in moduleObj.SupportedPlatformsArray) {
					AddNewConfiguration (platform);
				}
			}

			if (definition.Path != null) {
				BaseDirectory = Path.Combine (module.Path, definition.Path);

				// TODO See ReadProjectFile in MSBuildProjectHandler
				var filesNode = document.SelectSingleNode ("/Project/Files");
				if (filesNode != null) {
					foreach (var child in filesNode.ChildNodes.OfType<XmlElement> ()) {
						Items.Add (ImportFile (child));
					}
				}

				References = new ProtobuildReferences();

				var referencesNode = document.SelectSingleNode ("/Project/References");
				if (referencesNode != null) {
					foreach (var child in referencesNode.ChildNodes.OfType<XmlElement> ()) {
						References.Add (ImportReference (child));
					}
				}

				Dependencies = new ProtobuildDependencies();

				var dependenciesNode = document.SelectSingleNode("/Project/Dependencies");
				if (dependenciesNode != null)
				{
					foreach (var child in dependenciesNode.ChildNodes.OfType<XmlElement>())
					{
						Dependencies.Add(ImportDependency(child));
					}
				}

				Services = new ProtobuildServices();

				var servicesNode = document.SelectSingleNode("/Project/Services");
				if (servicesNode != null)
				{
					foreach (var child in servicesNode.ChildNodes.OfType<XmlElement>())
					{
						Services.Add(ImportService(child));
					}
				}
			}
		}

        private ProtobuildDependency ImportDependency (XmlElement child)
        {
            var dependency = new ProtobuildDependency();
            dependency.Name = child.GetAttribute("Name");
            dependency.Type = child.Name;
            return dependency;
        }

        private ProtobuildService ImportService (XmlElement child)
        {
            var service = new ProtobuildService ();
            service.Name = child.GetAttribute ("Name");
            // TODO Other properties
            return service;
        }

        public ProtobuildReferences References { get; set; }

        public ProtobuildDependencies Dependencies { get; set; }

        public ProtobuildServices Services { get; set; }

        private ProtobuildReference ImportReference (XmlElement child)
        {
            var reference = new ProtobuildReference ();
            reference.Name = child.GetAttribute ("Include");
            return reference;
        }

        #if MONODEVELOP_5

		public override string[] SupportedLanguages {
			get {
				return new[] { "C#", string.Empty };
			}
		}

        #endif

        private ProjectFile ImportFile (XmlElement child)
        {
            var file = new ProtobuildProjectFile();
            file.BuildAction = child.Name;

            var path = MSBuildProjectService.FromMSBuildPath(Path.Combine(module.Path, definition.Path), child.GetAttribute("Include"));
            file.Name = path;

            /*
            string dependentFile = buildItem.GetMetadata("DependentUpon");
            if (!string.IsNullOrEmpty(dependentFile))
            {
                dependentFile = MSBuildProjectService.FromMSBuildPath(Path.GetDirectoryName(path), dependentFile);
                file.DependsOn = dependentFile;
            }

            string copyToOutputDirectory = buildItem.GetMetadata("CopyToOutputDirectory");
            if (!string.IsNullOrEmpty(copyToOutputDirectory))
            {
                switch (copyToOutputDirectory)
                {
                    case "None": break;
                    case "Always": file.CopyToOutputDirectory = FileCopyMode.Always; break;
                    case "PreserveNewest": file.CopyToOutputDirectory = FileCopyMode.PreserveNewest; break;
                    default:
                        MonoDevelop.Core.LoggingService.LogWarning(
                            "Unrecognised value {0} for CopyToOutputDirectory MSBuild property",
                            copyToOutputDirectory);
                        break;
                }
            }

            if (buildItem.GetMetadataIsFalse("Visible"))
                file.Visible = false;


            string resourceId = buildItem.GetMetadata("LogicalName");
            if (!string.IsNullOrEmpty(resourceId))
                file.ResourceId = resourceId;

            string contentType = buildItem.GetMetadata("SubType");
            if (!string.IsNullOrEmpty(contentType))
                file.ContentType = contentType;

            string generator = buildItem.GetMetadata("Generator");
            if (!string.IsNullOrEmpty(generator))
                file.Generator = generator;

            string customToolNamespace = buildItem.GetMetadata("CustomToolNamespace");
            if (!string.IsNullOrEmpty(customToolNamespace))
                file.CustomToolNamespace = customToolNamespace;

            string lastGenOutput = buildItem.GetMetadata("LastGenOutput");
            if (!string.IsNullOrEmpty(lastGenOutput))
                file.LastGenOutput = lastGenOutput;
            */

            var linkNode = child.SelectSingleNode ("Link");
            if (linkNode != null) {
                var link = linkNode.InnerText;
                if (!string.IsNullOrEmpty (link)) {
                    if (!Platform.IsWindows)
                        link = MSBuildProjectService.UnescapePath (link);
                    file.Link = link;
                }
            }

            var platformsNode = child.SelectSingleNode("Platforms");
            var includePlatformsNode = child.SelectSingleNode("IncludePlatforms");
            var excludePlatformsNode = child.SelectSingleNode("ExcludePlatforms");
            if (platformsNode != null)
            {
                file.IncludePlatforms = platformsNode.InnerText.Split(',');
            }
            else if (includePlatformsNode != null)
            {
                file.IncludePlatforms = includePlatformsNode.InnerText.Split(',');
            }
            if (excludePlatformsNode != null)
            {
                file.ExcludePlatforms = excludePlatformsNode.InnerText.Split(',');
            }

            var servicesNode = child.SelectSingleNode("Services");
            var includeServicesNode = child.SelectSingleNode("IncludeServices");
            var excludeServicesNode = child.SelectSingleNode("ExcludeServices");
            if (servicesNode != null)
            {
                file.IncludeServices = servicesNode.InnerText.Split(',');
            }
            else if (includeServicesNode != null)
            {
                file.IncludeServices = includeServicesNode.InnerText.Split(',');
            }
            if (excludeServicesNode != null)
            {
                file.ExcludeServices = excludeServicesNode.InnerText.Split(',');
            }

            return file;
        }

		private XmlElement ExportFile(ProjectFile file, XmlDocument document)
		{
			var basePath = Path.Combine(module.Path, definition.Path);
			var node = document.CreateElement(file.BuildAction);
			node.SetAttribute("Include", MSBuildProjectService.ToMSBuildPath(basePath, file.FilePath));
			return node;
		}

		private void ExportStandardProject(XmlDocument document)
		{
			var filesNode = document.SelectSingleNode ("/Project/Files");
			filesNode.RemoveAll();

			foreach (var file in this.Files)
			{
				if (file.Subtype != Subtype.Directory)
				{
					filesNode.AppendChild(ExportFile(file, document));
				}
			}
		}

		#endregion

		#region Content projects

		private void ImportContentProject(XmlDocument document, IProtobuildModule module)
		{
			foreach (var platform in module.SupportedPlatformsArray) {
				AddNewConfiguration (platform);
			}

			var sourceNodes = document.SelectNodes ("/ContentProject/Source");

			foreach (var sourceNode in sourceNodes.OfType<XmlElement>())
			{
				Items.Add(ImportContentSourceRule(sourceNode));
			}
		}

		private ProjectItem ImportContentSourceRule(XmlElement sourceRuleXml)
		{
			var sourceRule = new ProtobuildContentSourceRule();
			sourceRule.Include = sourceRuleXml.GetAttribute("Include");
			sourceRule.Match = sourceRuleXml.GetAttribute("Match");
			sourceRule.Primary = sourceRuleXml.GetAttribute("Primary").ToLowerInvariant() == "true";
			return sourceRule;
		}

		private void ExportContentProject(XmlDocument document)
		{
		}

		#endregion

		#region External projects

		private void ImportExternalProject(XmlDocument document, IProtobuildModule module)
		{
			foreach (var platform in module.SupportedPlatformsArray) {
				AddNewConfiguration (platform);
			}

			var nodes = document.SelectNodes ("/ExternalProject/*");

			foreach (var node in nodes.OfType<XmlElement>())
			{
				ImportExternalProjectNode(x => this.Items.Add (x), node);
			}

		}

		private void ImportExternalProjectNode(Action<ProjectItem> add, XmlElement node)
		{
			var reference = new ProtobuildExternalRef();
			reference.Node = node;

			switch (node.Name)
			{
			case "Platform":
				var platformFilter = new ProtobuildPlatformFilter();
				platformFilter.Name = node.GetAttribute("Type");

				foreach (var child in node.ChildNodes.OfType<XmlElement>())
				{
					ImportExternalProjectNode (x =>
						{
							if (x is IProtobuildExternalRefOrServiceFilter)
							{ 
								platformFilter.Items.Add((IProtobuildExternalRefOrServiceFilter)x);
							}
						}, child);
				}

				add(platformFilter);
				return;
			case "Service":
				var serviceFilter = new ProtobuildServiceFilter();
				serviceFilter.Name = node.GetAttribute("Name");

				foreach (var child in node.ChildNodes.OfType<XmlElement>())
				{
					ImportExternalProjectNode (x =>
						{
							if (x is ProtobuildExternalRef)
							{ 
								serviceFilter.Items.Add((ProtobuildExternalRef)x);
							}
						}, child);
				}

				add(serviceFilter);
				return;
			case "Reference":
				// GAC or Protobuild Project reference
				reference.Name = node.GetAttribute("Include");
				reference.Type = ProtobuildExternalRefType.GACOrProtobuildProject;
				break;
			case "Binary":
				// Another .NET assembly on disk
				reference.Name = node.GetAttribute("Name");
				reference.Path = node.GetAttribute("Path");
				reference.Type = ProtobuildExternalRefType.DotNETBinary;
				break;
			case "NativeBinary":
				// A native binary (or other file) on disk
				reference.Path = node.GetAttribute("Path");
				reference.Type = ProtobuildExternalRefType.NativeBinary;
				break;
			case "Project":
				// An external C# project
				reference.Name = node.GetAttribute("Name");
				reference.Guid = node.GetAttribute("Guid");
				reference.Path = node.GetAttribute("Path");
				reference.Type = ProtobuildExternalRefType.MSBuildProject;
				break;
			default:
				// Not supported
				return;
			}

			add(reference);
		}

		private void ExportExternalProject(XmlDocument document)
		{
		}

		#endregion

		protected override Task OnSave (ProgressMonitor monitor)
		{
            return Task.Run(() =>
            {
    			var document = new XmlDocument ();
    			document.Load(definition.DefinitionPath);

    			switch (definition.Type) {
    			case "External":
    				ExportExternalProject(document);
    				break;
    			case "Content":
    				ExportContentProject(document);
    				break;
    			default:
    				ExportStandardProject(document);
    				break;
    			}

    			var settings = new XmlWriterSettings
    			{
    				Indent = true,
    				IndentChars = "  ",
    				NewLineChars = "\n",
    				Encoding = Encoding.UTF8
    			};
    			using (var memory = new MemoryStream())
    			{
    				using (var writer = XmlWriter.Create(memory, settings))
    				{
    					document.Save(writer);
    				}
    				memory.Seek(0, SeekOrigin.Begin);
    				var reader = new StreamReader(memory);
    				var content = reader.ReadToEnd().Trim() + Environment.NewLine;
    				using (var writer = new StreamWriter(definition.DefinitionPath, false, Encoding.UTF8))
    				{
    					writer.Write(content);
    				}
    			}

    			((ProtobuildModule)ParentSolution).DefinitionOrModuleSaved();
            });
		}

        public override FilePath FileName
        {
            get { return definition.DefinitionPath; }
            set { definition.DefinitionPath = value; /* TODO Sync */ }
        }

        protected override void OnSetName (string value)
        {
            base.OnSetName (value);

            definition.Name = value;

            // TODO: Sync
        }

		public string Type
		{
			get { return definition.Type; }
		}

        public string ProjectDirectory
        {
            get
            {
                if (definition.Path == null) {
                    // External projects do not have a definition path.
                    return module.Path;
                }
                else {
                    return Path.Combine (module.Path, definition.Path);
                }
            }
        }

        public override Task<TargetEvaluationResult> OnRunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
        {
            var module = (ProtobuildModule)ParentSolution;
            return RunDefinitionTarget(module, monitor, target, configuration, context);
        }

        private async Task<TargetEvaluationResult> RunDefinitionTarget(ProtobuildModule module, ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
        {
            var project = await module.GetShadowProject(this, monitor, configuration);
            var value = await project.OnRunTarget(monitor, target, configuration, context);
            if (target == ProjectService.BuildTarget) {
                module.OnDefinitionBuilt(this);
            }
            return value;
        }

        #if MONODEVELOP_5

        protected override void OnClean (ProgressMonitor monitor, ConfigurationSelector configuration)
        {
        }

        public override IEnumerable<string> GetProjectTypes ()
        {
            yield return "Protobuild";
        }

        protected override BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
        {
            var module = (ProtobuildModule)ParentSolution;
            var project = module.GetShadowProject(this, monitor, configuration);
			var result = project.Build(monitor, configuration);
			module.OnDefinitionBuilt(this);
			return result;
        }

        protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
        {
            var module = (ProtobuildModule)ParentSolution;
            var project = module.GetShadowProject(this, monitor, configuration);
            project.Execute(monitor, context, configuration);
        }

        #endif

        protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
        {
            // TODO I don't think we can use the shadow project here because it
            // might not be generated, and this method doesn't have IProgressMonitor
            // (which implies it's not a background operation).
            return definition.Type != "Library" && definition.Type != "External" && definition.Type != "Content";
        }

        public override Project GetProjectForTypeSystem ()
        {
            var module = (ProtobuildModule)ParentSolution;
            var project = module.GetShadowProject(this, ((ProtobuildModule)ParentSolution).ActiveConfiguration);
            if (project == null) {
                // This definition doesn't have a real .NET project backing it (e.g.
                // it is an external or content project).
                return this;
            }
            return project;
        }
    }
}
