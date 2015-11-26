// ProtobuildStandardDefinition.cs
//
// Copyright (c) 2015 June Rhodes
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
	public class ProtobuildStandardDefinition : Project, IProtobuildDefinition
	{
		protected readonly ProtobuildModuleInfo module;

		protected readonly ProtobuildDefinitionInfo definition;

		protected readonly XmlDocument document;

		protected readonly IProtobuildModule moduleObj;

		internal ProtobuildStandardDefinition (ProtobuildModuleInfo modulel, ProtobuildDefinitionInfo definitionl, XmlDocument document, IProtobuildModule moduleObj) {
			module = modulel;
			definition = definitionl;
			this.document = document;
			this.moduleObj = moduleObj;

			Initialize(this);
		}

		protected override void OnExtensionChainInitialized ()
		{
			base.OnExtensionChainInitialized ();

			Configurations.Clear ();

			switch (definition.Type) {
			case "External":
			case "Content":
				throw new NotSupportedException();
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

		Task IProtobuildDefinition.OnSave(ProgressMonitor monitor)
		{
			return this.OnSave(monitor);
		}

		protected override Task OnSave (ProgressMonitor monitor)
		{
			return Task.Run(() =>
				{
					var document = new XmlDocument ();
					document.Load(definition.DefinitionPath);

					switch (definition.Type) {
					case "External":
					case "Content":
						throw new NotSupportedException();
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

		protected override Task<TargetEvaluationResult> OnRunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			var module = (ProtobuildModule)ParentSolution;
			return RunDefinitionTarget(module, monitor, target, configuration, context);
		}

		private async Task<TargetEvaluationResult> RunDefinitionTarget(ProtobuildModule module, ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			var project = await module.GetShadowProject(this, monitor, configuration);
			var value = await project.RunTarget(monitor, target, configuration, context);
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

		public ProtobuildModule ModuleOwnerForConfiguration { get; set; }

		public override Project GetRealProject ()
		{
			if (ModuleOwnerForConfiguration == null) {
				return new MonoDevelop.CSharp.Project.CSharpProject();
			}
			var project = ModuleOwnerForConfiguration.GetShadowProject(this, ModuleOwnerForConfiguration.ActiveConfiguration);
			if (project == null) {
				// Always return a value so that the extensions are active for Protobuild projects.
				// TODO: Change this based on the type of the Protobuild project.
				return new MonoDevelop.CSharp.Project.CSharpProject();
			}
			return project;
		}
	}
}

