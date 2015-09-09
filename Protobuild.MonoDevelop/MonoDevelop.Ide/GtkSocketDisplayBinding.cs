//
// GtkSocketDisplayBinding.cs
//
// Author:
//       James Rhodes <jrhodes@redpointsoftware.com.au>
//
// Copyright (c) 2015 James Rhodes
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using System.Net.Sockets;
using System.Net;

namespace MonoDevelop.Ide
{
	public class GtkSocketDisplayBinding : IViewDisplayBinding
	{
		private GtkPlugOpenedFileList openedFileList;

		private GtkPlugBasedProtobuildEditorHost editor;

		private string extension;

		private uint? socketIDOnResume;

		public GtkSocketDisplayBinding(GtkPlugBasedProtobuildEditorHost editor, GtkPlugOpenedFileList openFileList, string fileExtension)
		{
			this.editor = editor;
			openedFileList = openFileList;
			extension = fileExtension;
		}

		public IViewContent CreateContent (MonoDevelop.Core.FilePath fileName, string mimeType, Project ownerProject)
		{
			var openFile = new GtkPlugOpenedFile();
			openFile.FileReference = fileName;
			openFile.SocketComms = new TcpListener(0);
			openFile.StartProcess = new Action(() =>
				{
					openFile.NetworkRequestLayer = new GtkPlugNetworkRequestLayer(openFile.SocketComms);
					Console.WriteLine("Instance process is listening on port " + ((IPEndPoint)openedFileList.SocketComms.Server.LocalEndPoint).Port);
					openFile.GtkPlugProcess = editor.SpawnHostProcessExecutable(openedFileList, 
						new string[]
						{
							"instance",
							((IPEndPoint)openFile.SocketComms.Server.LocalEndPoint).Port
								.ToString(System.Globalization.CultureInfo.InvariantCulture),
								//viewSocket.SocketID.ToString(System.Globalization.CultureInfo.InvariantCulture),
								fileName,
							});
					openFile.GtkPlugProcess.EnableRaisingEvents = true;
					openFile.NetworkRequestLayer.BlockUntilConnected();
				});
			openFile.StartProcess();
			openedFileList.OpenedFiles.Add(openFile);

			var viewSocket = new GtkSocketViewContent(openFile);
			return viewSocket;
		}

		public string Name {
			get {
				return "Protobuild IDE Editor: " + extension;
			}
		}

		public bool CanHandle (MonoDevelop.Core.FilePath fileName, string mimeType, Project ownerProject)
		{
			return fileName.Extension == extension;
		}

		public bool CanUseAsDefault {
			get {
				return true;
			}
		}
	}
}

