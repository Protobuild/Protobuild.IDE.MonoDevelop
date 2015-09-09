//
// GtkProtobuildModuleFolderPreviewWidget.cs
//
// Author:
//       James <>
//
// Copyright (c) 2015 James
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
using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.Protobuild
{
	[System.ComponentModel.ToolboxItem (true)]
    public partial class GtkProtobuildModuleFolderPreviewWidget : Gtk.Bin
	{
		const string FolderIconId = "md-open-folder";
		const string FileIconId = "md-empty-file-icon";

		const int TextColumn = 1;
		TreeStore folderTreeStore;
		TreeIter locationNode;
		TreeIter moduleFolderNode;
	    TreeIter buildFolderNode;
        TreeIter projectsFolderNode;
        TreeIter moduleXmlNode;
		TreeIter protobuildExecutableNode;
		TreeIter gitIgnoreNode;

        FinalProtobuildModuleConfigurationPage projectConfiguration;

        public GtkProtobuildModuleFolderPreviewWidget()
		{
			this.Build ();

			CreateFolderTreeViewColumns ();
		}

		void CreateFolderTreeViewColumns ()
		{
			folderTreeStore = new TreeStore (typeof(string), typeof(string));
			folderTreeView.Model = folderTreeStore;
			folderTreeView.ShowExpanders = false;
			folderTreeView.LevelIndentation = 10;
			folderTreeView.CanFocus = false;

			var column = new TreeViewColumn ();
			var iconRenderer = new CellRendererImage ();
			column.PackStart (iconRenderer, false);
			column.AddAttribute (iconRenderer, "stock-id", column: 0);

			var textRenderer = new CellRendererText ();
			textRenderer.Ellipsize = Pango.EllipsizeMode.Middle;
			column.PackStart (textRenderer, true);
			column.AddAttribute (textRenderer, "markup", TextColumn);

			folderTreeView.AppendColumn (column);
		}

        public void Load(FinalProtobuildModuleConfigurationPage projectConfiguration)
		{
			this.projectConfiguration = projectConfiguration;
			Refresh ();
		}

		public void Refresh ()
		{
			folderTreeStore.Clear ();

            locationNode = folderTreeStore.AppendValues(FolderIconId, string.Empty);

            moduleFolderNode = folderTreeStore.AppendValues(locationNode, FolderIconId, "Module");
            buildFolderNode = folderTreeStore.AppendValues(moduleFolderNode, FolderIconId, "Build");
            projectsFolderNode = folderTreeStore.AppendValues(buildFolderNode, FolderIconId, "Projects");
            moduleXmlNode = folderTreeStore.AppendValues(buildFolderNode, FileIconId, "Module.xml");
            protobuildExecutableNode = folderTreeStore.AppendValues(moduleFolderNode, FileIconId, "Protobuild.exe");

            gitIgnoreNode = AddGitIgnoreToTree();

			UpdateTreeValues ();

			folderTreeView.ExpandAll ();
		}

		void UpdateTreeValues ()
		{
			UpdateLocation ();
			UpdateSolutionName ();
			ShowGitIgnoreFile ();
		}

		TreeIter AddGitIgnoreToTree ()
		{
			TreeIter parent = moduleFolderNode;
			return folderTreeStore.InsertWithValues (parent, 0, FileIconId, ".gitignore");
		}

		public void UpdateLocation ()
		{
			UpdateTextColumn (locationNode, projectConfiguration.Location);
		}

		void UpdateTextColumn (TreeIter iter, string value)
		{
			if (!iter.Equals (TreeIter.Zero)) {
				folderTreeStore.SetValue (iter, TextColumn, GLib.Markup.EscapeText (value));
			}
		}

		public void UpdateSolutionName ()
		{
			string solutionName = projectConfiguration.GetValidSolutionName ();
			string solutionFileName = projectConfiguration.SolutionFileName;

			if (String.IsNullOrEmpty (solutionName)) {
				solutionName = "Module";
				solutionFileName = solutionName + solutionFileName;
			}

			if (ShowingSolutionFolderNode ()) {
				UpdateTextColumn (moduleFolderNode, solutionName);
			}
		}

		public void ShowGitIgnoreFile ()
		{
			if (projectConfiguration.IsGitIgnoreEnabled && projectConfiguration.CreateGitIgnoreFile && projectConfiguration.IsNewSolution) {
				if (gitIgnoreNode.Equals (TreeIter.Zero)) {
					gitIgnoreNode = AddGitIgnoreToTree ();
				}
			} else if (!gitIgnoreNode.Equals (TreeIter.Zero)) {
				folderTreeStore.Remove (ref gitIgnoreNode);
				gitIgnoreNode = TreeIter.Zero;
			}
		}

		bool ShowingSolutionFolderNode ()
		{
			return !moduleFolderNode.Equals (TreeIter.Zero);
		}
	}
}

