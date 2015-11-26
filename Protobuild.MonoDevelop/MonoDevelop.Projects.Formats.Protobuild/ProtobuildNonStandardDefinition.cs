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
	public class ProtobuildNonStandardDefinition : SolutionFolderItem, IProtobuildDefinition
	{
		protected readonly ProtobuildModuleInfo module;

		protected readonly ProtobuildDefinitionInfo definition;

		protected readonly XmlDocument document;

		protected readonly IProtobuildModule moduleObj;

		protected ProtobuildNonStandardDefinition (ProtobuildModuleInfo modulel, ProtobuildDefinitionInfo definitionl, XmlDocument document, IProtobuildModule moduleObj)
        {
            module = modulel;
			definition = definitionl;
			this.document = document;
			this.moduleObj = moduleObj;

			Items = new ProjectItemCollection();

			Initialize(this);
		}

		protected override string OnGetBaseDirectory ()
		{
			return moduleObj.RootFolder.BaseDirectory;
		}

		protected override string OnGetName ()
		{
			return definition.Name;
		}

		protected override void OnSetName (string value)
		{
		}

		public ProtobuildReferences References { get; set; }

		public ProtobuildDependencies Dependencies { get; set; }

		public ProtobuildServices Services { get; set; }

		public ProjectItemCollection Items { get; private set; }

		protected override void OnExtensionChainInitialized ()
		{
			base.OnExtensionChainInitialized ();

			switch (definition.Type) {
			case "External":
				ImportExternalProject(document, moduleObj);
				break;
			case "Content":
				ImportContentProject(document, moduleObj);
				break;
			default:
				throw new NotSupportedException();
			}

			base.Name = definition.Name;
		}

		#region Content projects

		private void ImportContentProject(XmlDocument document, IProtobuildModule module)
		{
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

		public Task OnSave (ProgressMonitor monitor)
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
						throw new NotSupportedException();
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
    }
}
