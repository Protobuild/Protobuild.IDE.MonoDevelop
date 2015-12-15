using System.Linq;
using MonoDevelop.Ide.Templates;
using System.Reflection;
using System.IO;
using System;
using Mono.Addins;
using MonoDevelop.Ide;

namespace Protobuild.MonoDevelop
{
    public class TemplateLoader
    {
        bool hadExistingParent = false;
        object runtimeAddin = null;

		public void LoadTemplateCategories()
		{
			try
			{
				var templatingServiceType = IdeApp.Services.TemplatingService.GetType();
				var onTemplateCategoriesChanged = templatingServiceType.GetMethod("OnTemplateCategoriesChanged", BindingFlags.NonPublic | BindingFlags.Instance);

				var dir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".xamarin-templates"));

                var existingParent = AddinManager.AddinEngine.GetExtensionNode("/MonoDevelop/Ide/ProjectTemplateCategories")
                    .ChildNodes.OfType<ExtensionNode>().FirstOrDefault(x => x.Id == "crossplat" || x.Id == "multiplat");

                //existingParent = null;

				var ideAssembly = typeof(TemplateWizard).Assembly;
				var templateCategoryCodonType = ideAssembly.GetTypes().First(x => x.Name == "TemplateCategoryCodon");
				var templateCategoryCodonConstructor = templateCategoryCodonType.GetConstructor(Type.EmptyTypes);
				var nameField = templateCategoryCodonType.GetField("name", BindingFlags.NonPublic | BindingFlags.Instance);
				var treeNodeField = typeof(ExtensionNode).GetField("treeNode", BindingFlags.NonPublic | BindingFlags.Instance);
				var iconField = templateCategoryCodonType.GetField("icon", BindingFlags.NonPublic | BindingFlags.Instance);
				var addinField = typeof(ExtensionNode).GetField("addin", BindingFlags.NonPublic | BindingFlags.Instance);

				var runtimeAddinType = typeof(Addin).Assembly.GetTypes().First(x => x.Name == "RuntimeAddin");
				var runtimeAddinConstructor = runtimeAddinType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First(x => x.GetParameters().Length == 1);
				var runtimeAddinIdField = runtimeAddinType.GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);

				var treeNodeType = typeof(Addin).Assembly.GetTypes().First(x => x.Name == "TreeNode");
				var treeNodeConstructor = treeNodeType.GetConstructor(new[] { typeof(AddinEngine), typeof(string) });
				var treeNodeAddChild = treeNodeType.GetMethod("AddChildNode", new[] { treeNodeType });
				var treeNodeExtensionNodeField = treeNodeType.GetField("extensionNode", BindingFlags.Instance | BindingFlags.NonPublic);

				object crossPlatformTemplateCategoryCodon;
				object crossPlatformTemplateCategoryTreeNode;
                if (existingParent == null)
				{
					crossPlatformTemplateCategoryTreeNode = treeNodeConstructor.Invoke(new object[] { AddinManager.AddinEngine, "crossplat" });

                    runtimeAddin = runtimeAddinConstructor.Invoke(new object[] { AddinManager.AddinEngine });
                    runtimeAddinIdField.SetValue(runtimeAddin, "ProtobuildTemplateCategoryLoader");

					crossPlatformTemplateCategoryCodon = templateCategoryCodonConstructor.Invoke(null);
					nameField.SetValue(crossPlatformTemplateCategoryCodon, "Cross-platform");
					iconField.SetValue(crossPlatformTemplateCategoryCodon, "md-platform-cross-platform");
					addinField.SetValue(crossPlatformTemplateCategoryCodon, runtimeAddin);
					treeNodeField.SetValue(crossPlatformTemplateCategoryCodon, crossPlatformTemplateCategoryTreeNode);
					treeNodeExtensionNodeField.SetValue(crossPlatformTemplateCategoryTreeNode, crossPlatformTemplateCategoryCodon);
				}
                else
                {
                    crossPlatformTemplateCategoryCodon = existingParent;
                    crossPlatformTemplateCategoryTreeNode = treeNodeField.GetValue(crossPlatformTemplateCategoryCodon);
                    runtimeAddin = addinField.GetValue(crossPlatformTemplateCategoryCodon);
                }

				foreach (var file in dir.GetFiles("*.category"))
				{
					var id = file.Name.Substring(0, file.Name.Length - file.Extension.Length);
					var name = string.Empty;
					using (var reader = new StreamReader(file.FullName))
					{
						name = reader.ReadToEnd().Trim();
					}

					var templateCategoryCodon = templateCategoryCodonConstructor.Invoke(null);
					nameField.SetValue(templateCategoryCodon, name);
					addinField.SetValue(templateCategoryCodon, runtimeAddin);
					var treeNodeChild = treeNodeConstructor.Invoke(new object[] { AddinManager.AddinEngine, id });
					treeNodeField.SetValue(templateCategoryCodon, treeNodeChild);
					treeNodeExtensionNodeField.SetValue(treeNodeChild, templateCategoryCodon);

					treeNodeAddChild.Invoke(crossPlatformTemplateCategoryTreeNode, new object[] { treeNodeChild });

					var genericTemplateCategoryCodon = templateCategoryCodonConstructor.Invoke(null);
					nameField.SetValue(genericTemplateCategoryCodon, "General");
					addinField.SetValue(genericTemplateCategoryCodon, runtimeAddin);
					var genericTreeNodeChild = treeNodeConstructor.Invoke(new object[] { AddinManager.AddinEngine, "general" });
					treeNodeField.SetValue(genericTemplateCategoryCodon, genericTreeNodeChild);
					treeNodeExtensionNodeField.SetValue(genericTreeNodeChild, genericTemplateCategoryCodon);

					treeNodeAddChild.Invoke(treeNodeChild, new object[] { genericTreeNodeChild });

					/*
					onTemplateCategoriesChanged.Invoke(IdeApp.Services.TemplatingService, new object[]
						{
							null,
							new ExtensionNodeEventArgs(ExtensionChange.Add, (ExtensionNode)templateCategoryCodon)
						});
						*/
				}

                if (existingParent == null)
                {
    				onTemplateCategoriesChanged.Invoke(IdeApp.Services.TemplatingService, new object[]
    					{
    						null,
    						new ExtensionNodeEventArgs(ExtensionChange.Add, (ExtensionNode)crossPlatformTemplateCategoryCodon)
    					});
                }
                else
                {
                    var childrenLoaded = typeof(ExtensionNode).GetField("childrenLoaded", BindingFlags.NonPublic | BindingFlags.Instance);
                    childrenLoaded.SetValue(crossPlatformTemplateCategoryCodon, false);

                    onTemplateCategoriesChanged.Invoke(IdeApp.Services.TemplatingService, new object[]
                        {
                            null,
                            new ExtensionNodeEventArgs(ExtensionChange.Remove, (ExtensionNode)crossPlatformTemplateCategoryCodon)
                        });

                    onTemplateCategoriesChanged.Invoke(IdeApp.Services.TemplatingService, new object[]
                        {
                            null,
                            new ExtensionNodeEventArgs(ExtensionChange.Add, (ExtensionNode)crossPlatformTemplateCategoryCodon)
                        });
                }
			}
			catch (Exception exx)
			{
				System.Diagnostics.Debug.Write(exx);
			}
		}

        public void LoadTemplates()
        {
            return;

            try
            {
                var ideAssembly = typeof(TemplateWizard).Assembly;
                var projectTemplateType = ideAssembly.GetTypes().First(x => x.Name == "ProjectTemplate");
                var onExtensionChangedMethod = projectTemplateType.GetMethod("OnExtensionChanged", BindingFlags.Static | BindingFlags.NonPublic);

                // Search for all external templates.
                var dir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".xamarin-templates"));

                var projectCodonType = ideAssembly.GetTypes().First(x => x.Name == "ProjectTemplateCodon");
                var projectCodonConstructor = projectCodonType.GetConstructor(Type.EmptyTypes);
                var fileField = projectCodonType.GetField("file", BindingFlags.NonPublic | BindingFlags.Instance);
                var addinField = typeof(ExtensionNode).GetField("addin", BindingFlags.NonPublic | BindingFlags.Instance);
                var runtimeAddinType = typeof(Addin).Assembly.GetTypes().First(x => x.Name == "RuntimeAddin");
                var runtimeAddinConstructor = runtimeAddinType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First(x => x.GetParameters().Length == 1);
                var baseDirectoryField = runtimeAddinType.GetField("baseDirectory", BindingFlags.NonPublic | BindingFlags.Instance);
                var idField = runtimeAddinType.GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);

                var runtimeAddinLocal = runtimeAddinConstructor.Invoke(new object[] { AddinManager.AddinEngine });
                baseDirectoryField.SetValue(runtimeAddinLocal, dir.FullName);
                idField.SetValue(runtimeAddinLocal, "ProtobuildTemplateLoader");

                foreach (var file in dir.GetFiles("*.xpt.xml"))
                {
                    try
                    {
                        var projectCodon = projectCodonConstructor.Invoke(null);
                        fileField.SetValue(projectCodon, file.Name);
                        addinField.SetValue(projectCodon, runtimeAddinLocal);

                        onExtensionChangedMethod.Invoke(null, new object[] {
                            null,
                            new ExtensionNodeEventArgs(ExtensionChange.Add, (ExtensionNode)projectCodon)
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Write(ex);
                    }
                }
            }
            catch (Exception exx)
            {
                System.Diagnostics.Debug.Write(exx);
            }
        }
    }
}

