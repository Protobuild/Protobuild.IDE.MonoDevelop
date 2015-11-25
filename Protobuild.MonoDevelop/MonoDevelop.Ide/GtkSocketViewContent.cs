//
// GtkSocketViewContent.cs
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

namespace MonoDevelop.Ide
{
	public class GtkSocketViewContent : AbstractViewContent
	{
		private Gtk.Socket socket;

		private Gtk.Label label;

		private GtkPlugOpenedFile openFile;

		private Gtk.Widget socketParent;

		private bool didInit;

		private bool readyForInit;

		private bool wantsInit;

		public GtkSocketViewContent(GtkPlugOpenedFile openFile)
		{
			this.openFile = openFile;
			this.openFile.ViewContent = this;
			label = new Gtk.Label("Initializing...");
			label.ParentSet += this.OnLabelParentChanged;
			label.ShowAll();
		}

		public override Gtk.Widget Control {
			get {
				return (Gtk.Widget)socket ?? (Gtk.Widget)label;
			}
		}

		public uint SocketID {
			get
			{
				if (socket == null)
				{
					throw new Exception("Socket not initialized (call Resume first).");
				}

				return socket.Id;
			}
		}

		public override string TabPageLabel {
			get {
				return "Content Editor";
			}
		}

        public override void Load (FileOpenInformation fileName)
		{
            openFile.NetworkRequestLayer.Load(fileName.FileName);
            ContentName = fileName.FileName;
		}

        public override void Save (FileSaveInformation fileName)
		{
            openFile.NetworkRequestLayer.Save(fileName.FileName);
            ContentName = fileName.FileName;
		}

		public bool IsSuspended { get; set; }

		public override void Dispose ()
		{
			if (this.openFile.ViewContent == this)
			{
				this.openFile.ViewContent = null;
			}

			if (openFile.GtkPlugProcess != null)
			{
				Console.WriteLine("Killing GTK process for " + openFile.FileReference);
				try
				{
					openFile.GtkPlugProcess.Kill();
				}
				catch (InvalidOperationException)
				{
					// Process has already exited.
				}
				openFile.NetworkRequestLayer.Stop();
				openFile.GtkPlugProcess = null;
			}

			base.Dispose ();
		}

		public override void Suspend ()
		{
			openFile.SuspendedState = openFile.NetworkRequestLayer.Suspend();
			//SocketIDOnResume = null;
			IsSuspended = true;
			if (socket != null)
			{
				socket.ParentSet -= OnParentChanged;
				socket.Realized -= OnRealized;
				((Gtk.VBox)socketParent).Remove(socket);
				socket = null;
			}

			if (openFile.GtkPlugProcess != null)
			{
				Console.WriteLine("Killing GTK process for " + openFile.FileReference);
				try
				{
					openFile.GtkPlugProcess.Kill();
				}
				catch (InvalidOperationException)
				{
					// Process has already exited.
				}
				openFile.NetworkRequestLayer.Stop();
				openFile.GtkPlugProcess = null;
			}

			base.Suspend ();
		}

		public override void Resume ()
		{
			Console.WriteLine("Gtk socket being reconstructed");
			didInit = false;
			readyForInit = false;
			wantsInit = false;
			var oldSocket = socket;
			socket = new Gtk.Socket();
			socket.ModifyBg(Gtk.StateType.Active, new Gdk.Color(0, 0, 0));
			socket.ModifyBg(Gtk.StateType.Insensitive, new Gdk.Color(0, 0, 0));
			socket.ModifyBg(Gtk.StateType.Normal, new Gdk.Color(0, 0, 0));
			socket.ModifyBg(Gtk.StateType.Prelight, new Gdk.Color(0, 0, 0));
			socket.ModifyBg(Gtk.StateType.Selected, new Gdk.Color(0, 0, 0));
			socket.ParentSet += this.OnParentChanged;
			socket.Realized += this.OnRealized;
			socket.ShowAll();
			if (socketParent == null)
			{
				socketParent = oldSocket.Parent;
			}
			foreach (var child in ((Gtk.VBox)socketParent).AllChildren)
			{
				((Gtk.VBox)socketParent).Remove((Gtk.Widget)child);
			}
			((Gtk.VBox)socketParent).Add(socket);
			Console.WriteLine("Gtk socket added to socket parent");
			readyForInit = true;
			if (wantsInit)
			{
				this.OnRealized(this, new EventArgs());
			}

			base.Resume ();
		}

		void OnParentChanged (object o, Gtk.ParentSetArgs args)
		{
			if (socket.Parent != null)
			{
				socketParent = socket.Parent;
			}
		}

		void OnLabelParentChanged (object o, Gtk.ParentSetArgs args)
		{
			if (label.Parent != null)
			{
				socketParent = label.Parent;
				label.ParentSet -= OnLabelParentChanged;
			}
		}

		void OnRealized (object sender, EventArgs e)
		{
			if (!didInit && readyForInit)
			{
				Console.WriteLine("OnRealized called for Gtk socket with socket ID " + socket.Id);
				if (openFile.GtkPlugProcess == null)
				{
					openFile.StartProcess();
				}
				openFile.NetworkRequestLayer.Resume(socket.Id, openFile.SuspendedState);
				didInit = true;
				IsSuspended = false;
			}
			else if (!readyForInit)
			{
				wantsInit = true;
			}
		}
	}
}

