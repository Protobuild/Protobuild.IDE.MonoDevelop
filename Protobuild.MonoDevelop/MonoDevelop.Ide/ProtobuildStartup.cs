using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Formats.Protobuild;

namespace MonoDevelop.Ide
{
    public class ProtobuildStartup : CommandHandler
    {
        private List<ProtobuildModule> registeredModules = new List<ProtobuildModule> (); 

		private IProtobuildEditorHost editorHost;

        protected override void Run ()
        {
			if (Platform.IsLinux)
			{
				editorHost = new GtkPlugBasedProtobuildEditorHost();
			}
			else
			{
				editorHost = new AppDomainBasedProtobuildEditorHost();
			}

            IdeApp.Workspace.SolutionLoaded += (sender, args) => {
                foreach (var module in IdeApp.Workspace.GetAllSolutions ().OfType<ProtobuildModule> ()) {
                    registeredModules.Add (module);
                    module.ShadowSolutionChanged += OnModuleOnShadowSolutionChanged;
					module.Generated += ReloadEditorHosts;
					module.Built += ReloadEditorHosts;
					module.DefinitionBuilt += ReloadEditorHosts;
                }
            };
            IdeApp.Workspace.SolutionUnloaded += (sender, args) =>
            {
                foreach (var module in registeredModules)
                {
					module.ShadowSolutionChanged -= OnModuleOnShadowSolutionChanged;
					module.Generated -= ReloadEditorHosts;
					module.Built -= ReloadEditorHosts;
					module.DefinitionBuilt -= ReloadEditorHosts;
                }
                registeredModules.Clear ();
            };
            IdeApp.Workspace.ActiveConfigurationChanged += (sender, args) =>
            {
                foreach (var module in IdeApp.Workspace.GetAllSolutions().OfType<ProtobuildModule>()) {
                    var module1 = module;
                    /*DispatchService.BackgroundDispatch(() => {
                        var message = "Starting generation of .NET projects for " +
                                      IdeApp.Workspace.ActiveConfigurationId + "...";
                        using (var statusMonitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor(message)) {*/
                    module1.Generate (new ProgressMonitor(), IdeApp.Workspace.ActiveConfiguration);
                            module1.SetActiveConfiguration (IdeApp.Workspace.ActiveConfigurationId);
                        //}
                    //});
                }
            };
        }

		private void ReloadEditorHosts(object sender, SolutionItemEventArgs e)
		{
			if (e.SolutionItem == null)
			{
				editorHost.Reload(e.Solution);
			}
			else
			{
				editorHost.Reload(e.SolutionItem);
			}
		}

        private void OnModuleOnShadowSolutionChanged (object o, SolutionItemEventArgs eventArgs)
        {
            /*TypeSystemService.UnloadAllProjects ();
            foreach (var project in eventArgs.Solution.GetAllSolutionItems<ProtobuildDefinition> ()) {
                TypeSystemService.LoadProject (project);
            }*/

            // Active documents automatically change over because the type system reloads.
        }

		public static void RegenerateShadowSolutions ()
		{
			foreach (var sol in IdeApp.Workspace.GetAllSolutions().OfType<ProtobuildModule>())
			{
				var module1 = sol;
				module1.AfterSave = () =>
				{
					DispatchService.BackgroundDispatch(() => {
						var message = "Starting generation of .NET projects for " +
							IdeApp.Workspace.ActiveConfigurationId + "...";
                        using (var statusMonitor = new ProgressMonitor()) {//IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor(message)) {
							module1.ClearShadowSolutions();
							module1.Generate (statusMonitor, IdeApp.Workspace.ActiveConfiguration);
							module1.SetActiveConfiguration (IdeApp.Workspace.ActiveConfigurationId);
						}
					});
				};
			}
		}
    }
}
