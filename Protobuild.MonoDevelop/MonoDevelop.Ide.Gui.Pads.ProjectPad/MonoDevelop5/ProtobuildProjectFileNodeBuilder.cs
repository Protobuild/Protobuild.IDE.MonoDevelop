using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Formats.Protobuild;

#if MONODEVELOP_5

using MonoDevelop.Ide.Gui.Pads.ProjectPad.Protobuild;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
    class ProtobuildProjectFileNodeBuilder : ProjectFileNodeBuilder
    {
        public override Type NodeDataType
        {
            get { return typeof(ProtobuildProjectFile); }
        }

        public override Type CommandHandlerType
        {
            get { return typeof (ProtobuildProjectFileNodeCommandHandler); }
        }

        public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
            base.BuildNode(treeBuilder, dataObject, nodeInfo);

            ProtobuildProjectFile projectFile = (ProtobuildProjectFile)dataObject;

            SolutionConfiguration conf = projectFile.Project.ParentSolution.GetConfiguration(IdeApp.Workspace.ActiveConfiguration);

            var notActive = false;

            if (projectFile.IncludePlatforms != null) {
                if (!projectFile.IncludePlatforms.Contains (conf.Id)) {
                    notActive = true;
                }
            }

            if (projectFile.ExcludePlatforms != null)
            {
                if (projectFile.ExcludePlatforms.Contains(conf.Id))
                {
                    notActive = true;
                }
            }

            if (notActive)
            {
                nodeInfo.DisabledStyle = true;
                nodeInfo.StatusSeverity = TaskSeverity.Information;
                nodeInfo.StatusMessage = GettextCatalog.GetString("File not active for " + conf.Name);
            }
        }
    }

    class ProtobuildProjectFileNodeCommandHandler : ProjectFileNodeCommandHandler
    {
        [CommandHandler(ProtobuildCommands.FileEditPlatformsAndServices)]
        public void EditPlatformsAndServices()
        {
        }
    }
}

#endif