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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using Gtk;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide
{
	public class AppDomainViewContent : AbstractViewContent
	{
		private DrawingArea area;

		private AppDomainOpenedFile openFile;

	    private AppDomainBasedProtobuildEditorHost editor;

	    private ProxiedGraphicsContext graphicsProxy;

        private ProxiedWindowInfo windowProxy;

        [SuppressUnmanagedCodeSecurity, DllImport("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gdk_win32_drawable_get_handle(IntPtr d);

	    private bool canInit;

	    private bool didInit;

	    private IntPtr lastPointer;

	    private int lastPointerStableFor;

	    public AppDomainViewContent(AppDomainBasedProtobuildEditorHost editor, AppDomainOpenedFile openFile)
		{
		    this.editor = editor;
			this.openFile = openFile;
         
            this.graphicsProxy = new ProxiedGraphicsContext (null);
            this.windowProxy = new ProxiedWindowInfo (null);

            GLib.Timeout.Add(16, this.OpenWhenStabilized);

            area = new DrawingArea();
	        area.ExposeEvent += (o, args) => {
                if (!canInit)
                {
	                canInit = true;
	            }
	        };
            area.ConfigureEvent += (o, args) =>
            {
                if (!IsSuspended)
                {
                    try
                    {
                        openFile.Behaviour.Resize(
                            area.Allocation.Width,
                            area.Allocation.Height);
                    }
                    catch
                    {
                    }
                }
	        };
            area.ShowAll ();

			this.IsDirty = true;
		}

	    private bool OpenWhenStabilized ()
        {
	        if (!canInit) {
	            return true;
	        }

	        if (area.GdkWindow == null) {
                if (!IsSuspended)
                {
					#if MONODEVELOP_6_PENDING
	                this.Suspend();
					#endif
	            }
	            return true;
	        }

            IntPtr ptr = gdk_win32_drawable_get_handle(area.GdkWindow.Handle);//gdk_win32_drawable_get_handle();

	        if (lastPointer == ptr) {
	            lastPointerStableFor++;
	        }
	        else {
                Debug.Write ("GTK# pointer changed from " + lastPointer + " to " + ptr + "!");
                Console.WriteLine("GTK# pointer changed from " + lastPointer + " to " + ptr + "!");
	            lastPointer = ptr;
	            lastPointerStableFor = 0;
	        }

            if (lastPointerStableFor > 5 && lastPointer != IntPtr.Zero)
            {
                if (this.IsSuspended || !didInit) {
                    Debug.Write ("GTK# pointer stabilized!");
                    Console.WriteLine ("GTK# pointer stabilized!");
                }
                if (this.IsSuspended)
                {
					#if MONODEVELOP_6_PENDING
                    this.Resume ();
					#endif
                }
                else if (!didInit) {
                    openFile.Behaviour.Resume (ptr, openFile.SuspendedState);
                }
                didInit = true;
	        }

	        return true;
        }

	    public override Gtk.Widget Control {
			get {
				return area;
			}
		}

		public override string TabPageLabel {
			get {
				return "Content Editor";
			}
		}

        public override void Load (FileOpenInformation fileName)
		{
		    if (openFile.Behaviour != null) {
                openFile.Behaviour.Load (fileName.FileName);
		    }
            ContentName = fileName.FileName;
		}

        public override void Save (FileSaveInformation fileName)
        {
		    if (openFile.Behaviour != null) {
                openFile.Behaviour.Save (fileName.FileName);
		    }
            ContentName = fileName.FileName;
		}

		public bool IsSuspended { get; set; }

		public override void Dispose ()
		{
		    openFile.Behaviour = null;
		    if (openFile.AppDomain != null) {
		        AppDomain.Unload (openFile.AppDomain);
		    }
		    openFile.AppDomain = null;
            
			base.Dispose ();
		}

		#if MONODEVELOP_6_PENDING

		public override void Suspend ()
        {
            openFile.SuspendedState = openFile.Behaviour.Suspend();
            IsSuspended = true;
            
            openFile.Behaviour = null;
            AppDomain.Unload(openFile.AppDomain);
            openFile.AppDomain = null;

			base.Suspend ();
		}

		public override void Resume ()
        {
		    if (openFile.AppDomain == null) {
		        openFile.StartAppDomain ();
		    }

            openFile.Behaviour.Resume(lastPointer, openFile.SuspendedState);
		    IsSuspended = false;
			
			base.Resume ();
		}

		#endif
	}
}

