//
// ProtobuildEditorHost.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Projects.Formats.Protobuild;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using MonoDevelop.Ide.Gui;
using OpenTK.Graphics;
using OpenTK.Platform;
using System.Diagnostics;
using Gtk;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace MonoDevelop.Ide
{
	/// <summary>
	/// Protobuild editor host that uses GTK plugs / sockets for hosting 
	/// editor implementations.  Used on Linux where AppDomain support in Mono
	/// causes native segmentation faults.
	/// </summary>
	public class GtkPlugBasedProtobuildEditorHost : ProtobuildEditorHost<GtkPlugOpenedFileList, GtkPlugOpenedFile>
    {
		private Dictionary<ProtobuildDefinition, GtkPlugOpenedFileList> openedFiles;

		private Dictionary<ProtobuildDefinition, DateTime> lastModified;

		private Dictionary<ProtobuildDefinition, List<IDisplayBinding>> registeredBindings;

		public GtkPlugBasedProtobuildEditorHost()
		{
			openedFiles = new Dictionary<ProtobuildDefinition, GtkPlugOpenedFileList>();
			lastModified = new Dictionary<ProtobuildDefinition, DateTime>();
			registeredBindings = new Dictionary<ProtobuildDefinition, List<IDisplayBinding>>();

			GLib.Timeout.Add(100, new GLib.TimeoutHandler(() => {
				foreach (var openFile in openedFiles)
				{
					var ex = openFile.Value.NetworkRequestLayer.GetAndResetLastException();
					if (ex != null)
					{
						Console.WriteLine(ex);
					}
				}
				return true;
			}));
		}

	    protected override void RegisterBinding (GtkPlugOpenedFileList openFileList, string ext)
		{
			var binding = new GtkSocketDisplayBinding(this, openFileList, ext);
			if (!registeredBindings.ContainsKey(openFileList.Definition))
			{
				registeredBindings[openFileList.Definition] = new List<IDisplayBinding>();
			}
			registeredBindings[openFileList.Definition].Add(binding);
			DisplayBindingService.RegisterRuntimeDisplayBinding(binding);
		}

        protected override GtkPlugOpenedFileList StartInfo(ProtobuildDefinition definition, string outputPath)
	    {
            var openedFileList = new GtkPlugOpenedFileList();
            openedFileList.Definition = definition;
            openedFileList.OpenedFiles = new List<GtkPlugOpenedFile>();
            openedFileList.SocketComms = new TcpListener(0);
            openedFileList.HostProcessPath = this.BuildHostProcessExecutableFor(outputPath);
	        return openedFileList;
	    }

	    protected override bool ShouldReload (GtkPlugOpenedFile openedFile)
	    {
	        return openedFile.GtkPlugProcess != null && openedFile.ViewContent != null &&
	               !openedFile.ViewContent.IsSuspended;
	    }

        protected override void SuspendEditor(ProtobuildDefinition definition, GtkPlugOpenedFile openedFile)
	    {
			#if MONODEVELOP_6_PENDING
            Console.WriteLine("Suspending editor for " + openedFile.FileReference + " in " + definition.Name);
            openedFile.ViewContent.Suspend();
			#endif
	    }

	    protected override void ResumeEditor (ProtobuildDefinition definition, GtkPlugOpenedFile openedFile)
	    {
			#if MONODEVELOP_6_PENDING
            Console.WriteLine("Resuming editor for " + openedFile.FileReference + " in " + definition.Name);
            openedFile.ViewContent.Resume();
			#endif
	    }

	    protected override void StopInfo (ProtobuildDefinition definition)
	    {
            if (openedFiles[definition].InfoProcess != null)
            {
                Console.WriteLine("Stopping info process in " + definition.Name);
                try
                {
                    openedFiles[definition].InfoProcess.Kill();
                }
                catch (InvalidOperationException)
                {
                }
                openedFiles[definition].NetworkRequestLayer.Stop();
            }
	    }

	    protected override void InitializeDefinitionAndRegisterBindings (ProtobuildDefinition definition,
	        GtkPlugOpenedFileList targetList)
	    {
            targetList.NetworkRequestLayer = new GtkPlugNetworkRequestLayer(targetList.SocketComms);
            targetList.InfoProcess = this.SpawnHostProcessExecutable(
                targetList,
                new[] {
					"info",
					((IPEndPoint)targetList.SocketComms.Server.LocalEndPoint).Port
						.ToString(System.Globalization.CultureInfo.InvariantCulture)
					});
            targetList.NetworkRequestLayer.BlockUntilConnected();

            foreach (var ext in targetList.NetworkRequestLayer.GetHandledFileExtensions())
            {
                RegisterBinding(targetList, ext);
            }
	    }

	    private string BuildHostProcessExecutableFor(string targetLibrary)
		{
			var filePath = Assembly.GetExecutingAssembly().Location;
			var dirPath = new FileInfo(filePath).DirectoryName;

			var code = @"
using System;
using System.IO;
using System.Reflection;

public static class Program
{
	private static string _path = """ + dirPath + @""";

	static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
	{
		string assemblyPath = Path.Combine(_path, new AssemblyName(args.Name).Name + "".dll"");
		if (File.Exists(assemblyPath) == false) return null;
		Assembly assembly = Assembly.LoadFrom(assemblyPath);
		return assembly;
	}

	public static void Main(string[] args)
	{
		AppDomain currentDomain = AppDomain.CurrentDomain;
		currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);

		var monoDevelopIde = Assembly.Load(""MonoDevelop.Ide"");
		var mType = monoDevelopIde.GetType(""MonoDevelop.Ide.GtkPlugBasedProtobuildEditorHost"");
		mType.GetMethod(""HostEntryPoint"", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[]
		{
			""" + targetLibrary + @""",
			args
		});
	}
}
";

			var parameters = new System.CodeDom.Compiler.CompilerParameters();
			parameters.GenerateExecutable = true;
			parameters.IncludeDebugInformation = true;

			var provider = new Microsoft.CSharp.CSharpCodeProvider();
			var results = provider.CompileAssemblyFromSource(parameters, code);

			if (results.Errors.Count > 0)
			{
				throw new Exception("Unable to compile host process executable!");
			}

			return results.PathToAssembly;
		}

		public Process SpawnHostProcessExecutable(GtkPlugOpenedFileList openedFiles, string[] args)
		{
			if (openedFiles.HostProcessPath == null)
			{
				throw new Exception("No host process built for this opened file list.");
			}

			// TODO Watch for exited events.
			var info = new ProcessStartInfo(
				"mono",
				"--debug " + openedFiles.HostProcessPath + " " + 
				string.Join(" " , args.Select(x => Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(x)))));
			return Process.Start(info);
		}

		public static void HostEntryPoint(string targetLibraryPath, string[] encodedArgs)
		{
			var args = encodedArgs.Select(x => System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(x))).ToArray();

			var isSuspended = false;
			var inSuspendedState = false;

			var communicationPort = int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
			Console.WriteLine("Gtk plug process running in mode " + args[0]);
			Console.WriteLine("Gtk plug process is connecting to port " + communicationPort);
			var client = new TcpClient();
			client.Connect(IPAddress.Loopback, communicationPort);
			var stream = client.GetStream();
			uint? changeSocketID = null;
			string pendingFileLoad = null;
			string pendingFileSave = null;
			byte[] suspendedState = null;

			var behaviour = new ProtobuildIDEEditorDomainBehaviour();
			behaviour.Init(targetLibraryPath);

			var thread = new Thread(new ThreadStart(() =>
				{
					while (true)
					{
						Console.WriteLine("Gtk plug process reading 1 byte...");
						var data = new byte[1];
						stream.Read(data, 0, 1);
						switch (data[0])
						{
						case (byte)'S':
							Console.WriteLine("Gtk plug process suspending...");
							// Suspend.
							isSuspended = true;
							while (!inSuspendedState)
							{
								Thread.Sleep(0);
							}
							Console.WriteLine("Gtk plug client is now suspended.");
							stream.Write(new byte[] { (byte)'S' }, 0, 1);
							var lenSBytes = BitConverter.GetBytes(suspendedState == null ? UInt32.MaxValue : (UInt32)suspendedState.Length);
							stream.Write(lenSBytes, 0, lenSBytes.Length);
							if (suspendedState != null)
							{
								stream.Write(suspendedState, 0, suspendedState.Length);
							}
							break;
						case (byte)'R':
							// Resume.
							Console.WriteLine("Gtk plug process resuming...");
							var newBytes = new byte[sizeof(uint)];
							stream.Read(newBytes, 0, newBytes.Length);
							changeSocketID = BitConverter.ToUInt32(newBytes, 0);
							var lenRBytes = new byte[sizeof(uint)];
							stream.Read(lenRBytes, 0, lenRBytes.Length);
							var stateLength = BitConverter.ToUInt32(lenRBytes, 0);
							byte[] stateData;
							if (stateLength == UInt32.MaxValue)
							{
								stateData = null;
							}
							else
							{
								stateData = new byte[stateLength];
								stream.Read(stateData, 0, stateData.Length);
							}
							suspendedState = stateData;
							isSuspended = false;
							while (inSuspendedState)
							{
								Thread.Sleep(0);
							}
							Console.WriteLine("Gtk plug client has now resumed.");
							stream.Write(new byte[] { (byte)'R' }, 0, 1);
							break;
						case (byte)'E':
							// get file extensions.
							Console.WriteLine("Gtk plug process getting handled file extensions...");
							var exts = behaviour.GetHandledFileExtensions();
							stream.Write(new byte[] { (byte)exts.Length }, 0, 1);
							foreach (var ext in exts)
							{
								stream.Write(new byte[] { (byte)ext.Length }, 0, 1);
								var b = System.Text.Encoding.ASCII.GetBytes(ext);
								stream.Write(b, 0, b.Length);
							}
							Console.WriteLine("Gtk plug wrote file extensions.");
							break;
						case (byte)'L':
							{
								// Load file.
								Console.WriteLine("Gtk plug process loading file...");
								var strLenBytes = new byte[sizeof(int)];
								stream.Read(strLenBytes, 0, strLenBytes.Length);
								var strLen = BitConverter.ToInt32(strLenBytes, 0);
								var stringBytes = new byte[strLen];
								stream.Read(stringBytes, 0, stringBytes.Length);
								pendingFileLoad = Encoding.ASCII.GetString(stringBytes);
								while (pendingFileLoad != null)
								{
									Thread.Sleep(0);
								}
								Console.WriteLine("Gtk plug client has now loaded file.");
								stream.Write(new byte[] { (byte)'L' }, 0, 1);
								break;
							}
						case (byte)'A':
							{
								// Save file.
								Console.WriteLine("Gtk plug process saving file...");
								var strLenBytes = new byte[sizeof(int)];
								stream.Read(strLenBytes, 0, strLenBytes.Length);
								var strLen = BitConverter.ToInt32(strLenBytes, 0);
								var stringBytes = new byte[strLen];
								stream.Read(stringBytes, 0, stringBytes.Length);
								pendingFileSave = Encoding.ASCII.GetString(stringBytes);
								isSuspended = false;
								while (pendingFileSave != null)
								{
									Thread.Sleep(0);
								}
								Console.WriteLine("Gtk plug client has now saved file.");
								stream.Write(new byte[] { (byte)'L' }, 0, 1);
								break;
							}
						case (byte)'X':
							{
								// Get and reset last exception.
								Console.WriteLine("Gtk getting and resetting last exception...");
								var ex = behaviour.GetAndResetLastException();
								var formatter = new BinaryFormatter();
								using (var memory = new MemoryStream())
								{
									formatter.Serialize(memory, ex);
									int len = (int)memory.Position;
									var lenBytes = BitConverter.GetBytes(len);
									stream.Write(new byte[] { (byte)'X' }, 0, 1);
									stream.Write(lenBytes, 0, lenBytes.Length);
									memory.Seek(0, SeekOrigin.Begin);
									memory.CopyTo(stream);
								}
								break;
							}
						default:
							throw new Exception("Unknown message received (did the host process exit?)");
						}
					}
				}));
			thread.Start();


			if (args[0] == "info")
			{
				// Handle information requests (such as what file types to
				// bind to).
				thread.Join();
			}
			else if (args[0] == "instance")
			{
				// Handle an actual editor instance.
				Gtk.Application.Init();

				var didInitBehaviour = false;

				Gtk.Plug plug = null;

				var graphicsProxy = new ProxiedGraphicsContext(null);
				var windowProxy = new ProxiedWindowInfo(null);

				var glWidget = new GLWidget();
				glWidget.ExposeEvent += (sender, e) => 
				{
					Console.WriteLine("Gtk plug widget got exposed event!");

					if (!didInitBehaviour)
					{
						graphicsProxy.SetTarget(glWidget.GraphicsContext);
						windowProxy.SetTarget(glWidget.WindowInfo);

						glWidget.GraphicsContext.MakeCurrent(glWidget.WindowInfo);
						//OpenTK.Graphics.OpenGL.GL.Begin(OpenTK.Graphics.OpenGL.PrimitiveType.Triangles);
						OpenTK.Graphics.OpenGL.GL.ClearColor(System.Drawing.Color.Black);
						//OpenTK.Graphics.OpenGL.GL.End();

						behaviour.Resume(graphicsProxy, windowProxy, suspendedState);

						// Perform a resize after the UI has time to get organised.
						GLib.Timeout.Add(10, new GLib.TimeoutHandler(() => {
							if (!isSuspended)
							{
								behaviour.Resize(
									glWidget.Allocation.Width,
									glWidget.Allocation.Height);
							}
							return false;
						}));

						didInitBehaviour = true;
					}
				};
				glWidget.ConfigureEvent += (sender, e) => 
				{
					if (!isSuspended)
					{
						try
						{
							behaviour.Resize(
								glWidget.Allocation.Width,
								glWidget.Allocation.Height);
						}
						catch
						{
						}
					}
				};

				GLib.Timeout.Add(16, new GLib.TimeoutHandler(() => {
					if (!isSuspended && changeSocketID.HasValue)
					{
						Console.WriteLine("Gtk plug process is not suspended and has change socket ID value");
						Console.WriteLine("Gtk plug process will now reparent GL widget onto new plug with socket ID " + changeSocketID.Value);

						// The Gtk plug has changed; we need to reparent the GL widget.
						glWidget.PoisonWindowHandle();
						didInitBehaviour = false;
						plug = new Gtk.Plug(changeSocketID.Value);
						plug.ModifyBg(Gtk.StateType.Active, new Gdk.Color(0, 0, 0));
						plug.ModifyBg(Gtk.StateType.Insensitive, new Gdk.Color(0, 0, 0));
						plug.ModifyBg(Gtk.StateType.Normal, new Gdk.Color(0, 0, 0));
						plug.ModifyBg(Gtk.StateType.Prelight, new Gdk.Color(0, 0, 0));
						plug.ModifyBg(Gtk.StateType.Selected, new Gdk.Color(0, 0, 0));
						plug.Add(glWidget);
						plug.ShowAll();
						changeSocketID = null;

						graphicsProxy.SetTarget(glWidget.GraphicsContext);
						windowProxy.SetTarget(glWidget.WindowInfo);

						Console.WriteLine("Reparenting complete!");
					}

					if (!isSuspended) {
						graphicsProxy.SetTarget(glWidget.GraphicsContext);
						windowProxy.SetTarget(glWidget.WindowInfo);

						if (pendingFileLoad != null)
						{
							behaviour.Load(pendingFileLoad);
							pendingFileLoad = null;
						}

						if (pendingFileSave != null)
						{
							behaviour.Save(pendingFileSave);
							pendingFileSave = null;
						}

						behaviour.Update(); 
					} else {
						if (!inSuspendedState)
						{
							suspendedState = behaviour.Suspend();
							if (plug != null)
							{
								plug.Remove(glWidget);
								plug.Dispose();
								plug = null;
							}
						}
					}
					inSuspendedState = isSuspended;
					return true;
				}));

				Application.Run();
			}
		}
	}
}

