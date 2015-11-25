using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Projects.Protobuild;
using MonoDevelop.Protobuild;

namespace MonoDevelop.Ide.Commands
{
    public enum ProtobuildCommands
    {
        FileEditPlatformsAndServices,
        ProjectEditPlatformsAndServices,
        ModuleEditPlatforms,
        PackageUpgradeAll,
        PackageUpgrade,
        PackageModifyReference,
        PackageEditRedirects,
        PackageSwapToSource,
        PackageSwapToBinary,
        PackageAdd,
        PackageDelete,
        NewModule
    }

    //MonoDevelop.Ide.Commands.FileCommands.NewProtobuildModuleHandler
    public class NewProtobuildModuleHandler : CommandHandler
    {
        protected override void Run()
        {
            #if MONODEVELOP_5
            var newProjectDialog = new NewProtobuildModuleDialog();
            newProjectDialog.OpenSolution = true;
            newProjectDialog.Show();
            #endif
        }
    }
	
}
