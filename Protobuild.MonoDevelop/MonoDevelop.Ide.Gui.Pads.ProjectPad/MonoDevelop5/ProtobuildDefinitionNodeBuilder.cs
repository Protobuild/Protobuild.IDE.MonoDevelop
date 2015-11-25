using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Formats.Protobuild;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;

#if FALSE

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
    class ProtobuildDefinitionNodeBuilder : FolderNodeBuilder
	{
		ProjectFileEventHandler fileAddedHandler;
		ProjectFileEventHandler fileRemovedHandler;
		ProjectFileRenamedEventHandler fileRenamedHandler;
		ProjectFileEventHandler filePropertyChangedHandler;
		SolutionItemModifiedEventHandler projectChanged;
		EventHandler<FileEventArgs> deletedHandler;

        public override Type NodeDataType
        {
            get { return typeof (ProtobuildDefinition); }
        }

        public override Type CommandHandlerType
        {
            get { return typeof(ProtobuildDefinitionNodeCommandHandler); }
        }

        public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
        {
            return ((ProtobuildDefinition) dataObject).Name;
        }

		protected override void Initialize ()
		{
			fileAddedHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (OnAddFile));
			fileRemovedHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (OnRemoveFile));
			filePropertyChangedHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (OnFilePropertyChanged));
			fileRenamedHandler = (ProjectFileRenamedEventHandler) DispatchService.GuiDispatch (new ProjectFileRenamedEventHandler (OnRenameFile));
			projectChanged = (SolutionItemModifiedEventHandler) DispatchService.GuiDispatch (new SolutionItemModifiedEventHandler (OnProjectModified));
			deletedHandler = (EventHandler<FileEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileEventArgs> (OnSystemFileDeleted));

			IdeApp.Workspace.FileAddedToProject += fileAddedHandler;
			IdeApp.Workspace.FileRemovedFromProject += fileRemovedHandler;
			IdeApp.Workspace.FileRenamedInProject += fileRenamedHandler;
			IdeApp.Workspace.FilePropertyChangedInProject += filePropertyChangedHandler;
			IdeApp.Workspace.ActiveConfigurationChanged += IdeAppWorkspaceActiveConfigurationChanged;
			FileService.FileRemoved += deletedHandler;
		}

		public override void Dispose ()
		{
			IdeApp.Workspace.FileAddedToProject -= fileAddedHandler;
			IdeApp.Workspace.FileRemovedFromProject -= fileRemovedHandler;
			IdeApp.Workspace.FileRenamedInProject -= fileRenamedHandler;
			IdeApp.Workspace.FilePropertyChangedInProject -= filePropertyChangedHandler;
			IdeApp.Workspace.ActiveConfigurationChanged -= IdeAppWorkspaceActiveConfigurationChanged;
			FileService.FileRemoved -= deletedHandler;
		}

		public override void OnNodeAdded (object dataObject)
		{
			base.OnNodeAdded (dataObject);
			ProtobuildDefinition project = (ProtobuildDefinition) dataObject;
			project.Modified += projectChanged;
		}

		public override void OnNodeRemoved (object dataObject)
		{
			base.OnNodeRemoved (dataObject);
			ProtobuildDefinition project = (ProtobuildDefinition) dataObject;
			project.Modified -= projectChanged;
		}

        public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
            base.BuildNode(treeBuilder, dataObject, nodeInfo);

			ProtobuildDefinition definition = (ProtobuildDefinition)dataObject;
            string escapedProjectName = GLib.Markup.EscapeText(definition.Name);

            nodeInfo.Icon = Context.GetIcon(definition.StockIcon);
            if (definition.ParentSolution != null && definition.ParentSolution.SingleStartup && definition.ParentSolution.StartupItem == definition)
                nodeInfo.Label = "<b>" + escapedProjectName + "</b>";
            else
                nodeInfo.Label = escapedProjectName;

            // Gray out the project name if it is not selected in the current build configuration

            SolutionConfiguration conf = definition.ParentSolution.GetConfiguration(IdeApp.Workspace.ActiveConfiguration);
            if (!definition.GetConfigurations().Contains(conf.Id)) {
                nodeInfo.DisabledStyle = true;
                nodeInfo.StatusSeverity = TaskSeverity.Information;
                nodeInfo.StatusMessage = GettextCatalog.GetString("Project does not support " + conf.Name);
            }
        }

        public override string GetFolderPath (object dataObject)
        {
            var definition = (ProtobuildDefinition) dataObject;
            return Path.Combine (definition.BaseDirectory);
        }

        public override void BuildChildNodes(ITreeBuilder builder, object dataObject)
        {
            var definition = (ProtobuildDefinition)dataObject;

            if (definition.References != null) {
                builder.AddChild (definition.References);
            }

            if (definition.Dependencies != null)
            {
                builder.AddChild(definition.Dependencies);
            }

            if (definition.Services != null)
            {
                builder.AddChild(definition.Services);
            }

			foreach (var contentRule in definition.Items.OfType<ProtobuildContentSourceRule>())
			{
				builder.AddChild(contentRule);
			}

			foreach (var platformFilter in definition.Items.OfType<ProtobuildPlatformFilter>())
			{
				builder.AddChild(platformFilter);
			}

			foreach (var serviceFilter in definition.Items.OfType<ProtobuildServiceFilter>())
			{
				builder.AddChild(serviceFilter);
			}

			foreach (var externalRef in definition.Items.OfType<ProtobuildExternalRef>())
			{
				builder.AddChild(externalRef);
			}

            base.BuildChildNodes(builder, dataObject);
        }

        public override bool HasChildNodes(ITreeBuilder builder, object dataObject)
        {
            return true;
        }

		#region General Event Handling (same as ProjectNodeBuilder)

		void OnAddFile (object sender, ProjectFileEventArgs args)
		{
			if (args.CommonProject != null && args.Count > 2 && args.SingleVirtualDirectory) {
				ITreeBuilder tb = GetFolder (args.CommonProject, args.CommonVirtualRootDirectory);
				if (tb != null)
					tb.UpdateChildren ();
			}
			else {
				foreach (ProjectFileEventInfo e in args)
					AddFile (e.ProjectFile, e.Project);
			}

			ProtobuildStartup.RegenerateShadowSolutions();
		}

		void OnRemoveFile (object sender, ProjectFileEventArgs args)
		{
			foreach (ProjectFileEventInfo e in args)
				RemoveFile (e.ProjectFile, e.Project);

			ProtobuildStartup.RegenerateShadowSolutions();
		}

		void OnSystemFileDeleted (object sender, FileEventArgs args)
		{
			if (!args.Any (f => f.IsDirectory))
				return;

			// When a folder is deleted, we need to remove all references in the tree, for all projects
			ITreeBuilder tb = Context.GetTreeBuilder ();
			var dirs = args.Where (d => d.IsDirectory).Select (d => d.FileName).ToArray ();

			foreach (var p in IdeApp.Workspace.GetAllProjects ()) {
				foreach (var dir in dirs) {
					if (tb.MoveToObject (new ProjectFolder (dir, p)) && tb.MoveToParent ())
						tb.UpdateAll ();
				}
			}

			ProtobuildStartup.RegenerateShadowSolutions();
		}
			
		void AddFile (ProjectFile file, Project project)
		{
			ITreeBuilder tb = Context.GetTreeBuilder ();

			if (file.DependsOnFile != null) {
				if (!tb.MoveToObject (file.DependsOnFile)) {
					// The parent is not in the tree. Add it now, and it will add this file as a child.
					AddFile (file.DependsOnFile, project);
				}
				else
					tb.AddChild (file);
				return;
			}

			object data;
			if (file.Subtype == Subtype.Directory)
				data = new ProjectFolder (file.Name, project);
			else
				data = file;

			// Already there?
			if (tb.MoveToObject (data))
				return;

			string filePath = file.IsLink
				? project.BaseDirectory.Combine (file.ProjectVirtualPath).ParentDirectory
				: file.FilePath.ParentDirectory;

			tb = GetFolder (project, filePath);
			if (tb != null)
				tb.AddChild (data);
		}

		ITreeBuilder GetFolder (Project project, FilePath filePath)
		{
			ITreeBuilder tb = Context.GetTreeBuilder ();
			if (filePath != project.BaseDirectory) {
				if (tb.MoveToObject (new ProjectFolder (filePath, project))) {
					return tb;
				}
				else {
					// Make sure there is a path to that folder
					tb = FindParentFolderNode (filePath, project);
					if (tb != null) {
						tb.UpdateChildren ();
						return null;
					}
				}
			} else {
				if (tb.MoveToObject (project))
					return tb;
			}
			return null;
		}

		ITreeBuilder FindParentFolderNode (string path, Project project)
		{
			int i = path.LastIndexOf (Path.DirectorySeparatorChar);
			if (i == -1) return null;

			string basePath = path.Substring (0, i);

			if (basePath == project.BaseDirectory)
				return Context.GetTreeBuilder (project);

			ITreeBuilder tb = Context.GetTreeBuilder (new ProjectFolder (basePath, project));
			if (tb != null) return tb;

			return FindParentFolderNode (basePath, project);
		}

		void RemoveFile (ProjectFile file, Project project)
		{
			ITreeBuilder tb = Context.GetTreeBuilder ();

			if (file.Subtype == Subtype.Directory) {
				if (!tb.MoveToObject (new ProjectFolder (file.Name, project)))
					return;
				tb.MoveToParent ();
				tb.UpdateAll ();
				return;
			} else {
				if (tb.MoveToObject (file)) {
					tb.Remove (true);
				} else {
					// We can't use IsExternalToProject here since the ProjectFile has
					// already been removed from the project
					string parentPath = file.IsLink
						? project.BaseDirectory.Combine (file.Link.IsNullOrEmpty? file.FilePath.FileName : file.Link.ToString ()).ParentDirectory
						: file.FilePath.ParentDirectory;

					if (!tb.MoveToObject (new ProjectFolder (parentPath, project)))
						return;
				}
			}

			while (tb.DataItem is ProjectFolder) {
				ProjectFolder f = (ProjectFolder) tb.DataItem;
				if (!Directory.Exists (f.Path) && !project.Files.GetFilesInVirtualPath (f.Path.ToRelative (project.BaseDirectory)).Any ())
					tb.Remove (true);
				else
					break;
			}
		}

		void OnRenameFile (object sender, ProjectFileRenamedEventArgs args)
		{
			foreach (ProjectFileEventInfo e in args) {
				ITreeBuilder tb = Context.GetTreeBuilder (e.ProjectFile);
				if (tb != null) tb.Update ();
			}

			ProtobuildStartup.RegenerateShadowSolutions();
		}

		void OnProjectModified (object sender, SolutionItemModifiedEventArgs args)
		{
			foreach (SolutionItemModifiedEventInfo e in args) {
				if (e.Hint == "References" || e.Hint == "Files")
					continue;
				ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
				if (tb != null) {
					if (e.Hint == "BaseDirectory" || e.Hint == "TargetFramework")
						tb.UpdateAll ();
					else
						tb.Update ();
				}
			}

			ProtobuildStartup.RegenerateShadowSolutions();
		}

		static HashSet<string> propertiesThatAffectDisplay = new HashSet<string> (new string[] { null, "DependsOn", "Link", "Visible" });
		void OnFilePropertyChanged (object sender, ProjectFileEventArgs e)
		{
			foreach (var project in e.Where (x => propertiesThatAffectDisplay.Contains (x.Property)).Select (x => x.Project).Distinct ()) {
				ITreeBuilder tb = Context.GetTreeBuilder (project);
				if (tb != null) tb.UpdateAll ();
			}

			ProtobuildStartup.RegenerateShadowSolutions();
		}

		void IdeAppWorkspaceActiveConfigurationChanged (object sender, EventArgs e)
		{
			foreach (Project p in IdeApp.Workspace.GetAllProjects ()) {
				ITreeBuilder tb = Context.GetTreeBuilder (p);
				if (tb != null)
				{ 
					tb.Update ();
					tb.UpdateChildren();
				}
			}
		}

		#endregion
    }

	class ProtobuildDefinitionNodeCommandHandler : FolderCommandHandler
	{
		[CommandUpdateHandler (ProjectCommands.SetAsStartupProject)]
		public void UpdateSetAsStartupProject (CommandInfo ci)
		{
			Project project = (Project) CurrentNode.DataItem;
			ci.Visible = project.CanExecute (new ExecutionContext (Runtime.ProcessService.DefaultExecutionHandler, null, IdeApp.Workspace.ActiveExecutionTarget), IdeApp.Workspace.ActiveConfiguration);
		}

		[CommandHandler (ProjectCommands.SetAsStartupProject)]
		public void SetAsStartupProject ()
		{
			Project project = CurrentNode.DataItem as Project;
			project.ParentSolution.SingleStartup = true;
			project.ParentSolution.StartupItem = project;
			IdeApp.ProjectOperations.Save (project.ParentSolution);
		}

		public override string GetFolderPath (object dataObject)
		{
			return ((ProtobuildDefinition)dataObject).ProjectDirectory;
		}
    }
}

#endif