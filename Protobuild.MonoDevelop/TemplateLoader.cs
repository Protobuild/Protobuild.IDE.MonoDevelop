using System.Linq;
using MonoDevelop.Ide.Templates;
using System.Reflection;
using System.IO;
using System;
using Mono.Addins;

namespace Protobuild.MonoDevelop
{
    public class TemplateLoader
    {
        public TemplateLoader ()
        {
            var ideAssembly = typeof(TemplateWizard).Assembly;
            var projectTemplateType = ideAssembly.GetTypes().First(x => x.Name == "ProjectTemplate");
            var onExtensionChangedMethod = projectTemplateType.GetMethod("OnExtensionChanged", BindingFlags.Static | BindingFlags.NonPublic);

            // Search for all external templates.
            var dir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".xamarin-templates"));

            // TODO
            //  * Construct a ProjectTemplateCodon for each file found on disk
            //  * Invoke OnExtensionChangedMethod with the codon as the ExtensionNode
            ExtensionNode node = null /* TODO codon */;

            onExtensionChangedMethod.Invoke(null, new object[] {
                null,
                new ExtensionNodeEventArgs(ExtensionChange.Add, node)
            });
        }
    }
}

