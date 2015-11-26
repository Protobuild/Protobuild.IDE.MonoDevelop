//
// ProtobuildAppDomain.cs
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MonoDevelop.Core;
using System.Diagnostics;

namespace MonoDevelop.Projects.Formats.Protobuild
{
    public class ProtobuildAppDomain
    {
		readonly string protobuildPath;

		FileSystemWatcher fileWatcher;

		AppDomain appDomain;

		ProtobuildDomainBehaviour domainBehaviour;

		public event EventHandler ProtobuildChangedEvent;

		public ProtobuildAppDomain (string path)
        {
			protobuildPath = path;
			var fileInfo = new FileInfo(path);
			fileWatcher = new FileSystemWatcher(fileInfo.DirectoryName, fileInfo.Name);
			fileWatcher.Changed += ProtobuildChanged;

			LoadAppDomain();
        }

		void ProtobuildChanged (object sender, FileSystemEventArgs e)
		{
			UnloadAppDomain();
			LoadAppDomain();

			if (ProtobuildChangedEvent != null)
			{
				ProtobuildChangedEvent(this, new EventArgs());
			}
		}

        public string HostPlatform { get; set; }

		void LoadAppDomain ()
		{
            var extensionPath = new FileInfo(typeof(ProtobuildDomainBehaviour).Assembly.Location).DirectoryName;
            var commonPath = string.Empty;
            for (var i = 0; i < AppDomain.CurrentDomain.BaseDirectory.Length; i++)
            {
                if (i >= extensionPath.Length)
                {
                    break;
                }

                if (AppDomain.CurrentDomain.BaseDirectory[i] == extensionPath[i])
                {
                    commonPath += extensionPath[i];
                }
            }

            var relToBaseApp = AppDomain.CurrentDomain.BaseDirectory.Substring(commonPath.Length);
            var relToExtensionPath = extensionPath.Substring(commonPath.Length);

			appDomain = AppDomain.CreateDomain(
				"Protobuild Executable", 
				null,
                commonPath,
                relToBaseApp + Path.PathSeparator + relToExtensionPath,
				true);

			// Modern versions of Protobuild pack their internal behaviour into
			// an LZMA compressed embedded resource, with the LZMA decompressor
			// as a class inside the Protobuild executable.

			domainBehaviour = (ProtobuildDomainBehaviour)
				appDomain.CreateInstanceAndUnwrap(
					typeof(ProtobuildDomainBehaviour).Assembly.FullName,
					typeof(ProtobuildDomainBehaviour).FullName);
			domainBehaviour.Init(protobuildPath);

		    HostPlatform = domainBehaviour.GetHostPlatform ();
		}

		public void UnloadAppDomain()
		{
			if (appDomain == null)
			{
				return;
			}

			domainBehaviour = null;
			AppDomain.Unload(appDomain);
			appDomain = null;
		}

        public ProtobuildModuleInfo LoadModule(ProgressMonitor monitor)
		{
			var rootPath = new FileInfo(protobuildPath).DirectoryName;
            return domainBehaviour.LoadModule(rootPath);
		}

		public void RunExecutableWithArguments(ProtobuildModuleInfo module, string args, Action<string> lineOutputted)
		{
			var protobuildPath = System.IO.Path.Combine(module.Path, "Protobuild.exe");

			try
			{
				var chmodStartInfo = new ProcessStartInfo
				{
					FileName = "chmod",
					Arguments = "a+x Protobuild.exe",
					WorkingDirectory = module.Path,
					CreateNoWindow = true,
					UseShellExecute = false
				};
				Process.Start(chmodStartInfo);
			}
			catch
			{
			}

			if (File.Exists(protobuildPath))
			{
				var pi = new ProcessStartInfo
				{
					FileName = protobuildPath,
					Arguments = args,
					WorkingDirectory = module.Path,
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					UseShellExecute = false
				};
				var p = new Process { StartInfo = pi };
				p.OutputDataReceived += (sender, eventArgs) =>
				{
					if (!string.IsNullOrEmpty(eventArgs.Data))
					{
						lineOutputted(eventArgs.Data);
						//monitor.Log.WriteLine(eventArgs.Data);
					}
				};
				p.ErrorDataReceived += (sender, eventArgs) =>
				{
					if (!string.IsNullOrEmpty(eventArgs.Data))
					{
						lineOutputted(eventArgs.Data);
						//monitor.Log.WriteLine(eventArgs.Data);
					}
				};
				p.Start();
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				p.WaitForExit();

				if (p.ExitCode == 0) {
					lineOutputted("Protobuild.exe " + args + " completed with exit code 0.");
				} else {
					lineOutputted("Protobuild.exe " + args + " failed with non-zero exit code.");
				}
			}
        }

        public void SaveModule(ProtobuildModuleInfo latestModuleInfo)
        {
            domainBehaviour.SaveModule(latestModuleInfo);
        }

		public class ProtobuildDomainBehaviour : MarshalByRefObject
		{
			private Assembly internalAssembly;

			public void Init (string path)
			{
				Assembly.LoadFrom(path);

				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				Stream stream = null;
				Assembly assembly = null;
				foreach (var a in assemblies)
				{
					var s = a.GetManifestResourceStream("Protobuild.Internal.dll.lzma");
					if (s != null)
					{
						stream = s;
						assembly = a;
						break;
					}
				}

				if (stream == null) 
				{
					throw new InvalidOperationException("Compressed LZMA version of Protobuild.Internal not found!");
				}

				var memory = new MemoryStream();
				var lzmaHelperType = assembly.GetType("LZMA.LzmaHelper", true);
				var lzmaDecompressMethod = lzmaHelperType.GetMethod("Decompress");
				lzmaDecompressMethod.Invoke(null, new object[] { stream, memory, null });

				var bytes = new byte[memory.Position];
				memory.Seek(0, SeekOrigin.Begin);
				memory.Read(bytes, 0, bytes.Length);
				memory.Close();

				internalAssembly = Assembly.Load(bytes);
			}

            public ProtobuildModuleInfo LoadModule(string rootPath) 
			{
				var moduleInfoType = internalAssembly.GetType("Protobuild.ModuleInfo");
				var moduleInfoLoad = moduleInfoType.GetMethod("Load");
				dynamic moduleRef = moduleInfoLoad.Invoke(null, new object[] { Path.Combine(rootPath, "Build", "Module.xml") });

			    /*if (monitor != null) {
			        monitor.BeginStepTask ("Loading " + moduleRef.Name + "...", 1, 5);
			    }*/

			    var moduleInfo = new ProtobuildModuleInfo();
                moduleInfo.Name = moduleRef.Name;
				moduleInfo.Path = moduleRef.Path;
			    moduleInfo.DefaultAction = moduleRef.DefaultAction;
                moduleInfo.DefaultLinuxPlatforms = moduleRef.DefaultLinuxPlatforms;
                moduleInfo.DefaultWindowsPlatforms = moduleRef.DefaultWindowsPlatforms;
                moduleInfo.DefaultMacOSPlatforms = moduleRef.DefaultMacOSPlatforms;
                moduleInfo.DisableSynchronisation = moduleRef.DisableSynchronisation;
                moduleInfo.GenerateNuGetRepositories = moduleRef.GenerateNuGetRepositories;
                moduleInfo.SupportedPlatforms = moduleRef.SupportedPlatforms;
			    moduleInfo.DefaultStartupProject = moduleRef.DefaultStartupProject;

                moduleInfo.LoadedDefinitions = new List<ProtobuildDefinitionInfo> ();

			    foreach (var definition in moduleRef.GetDefinitions ()) {
			        var definitionInfo = new ProtobuildDefinitionInfo ();
					definitionInfo.Name = definition.Name;
					try
					{
						// Newer versions of Protobuild name this property RelativePath.
						definitionInfo.Path = definition.RelativePath;
					}
					catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
					{
						definitionInfo.Path = definition.Path;
					}
			        definitionInfo.Type = definition.Type;
			        definitionInfo.DefinitionPath = definition.DefinitionPath;
			        definitionInfo.ModulePath = definition.ModulePath;
			        definitionInfo.SkipAutopackage = definition.SkipAutopackage;
			        moduleInfo.LoadedDefinitions.Add (definitionInfo);
                }

                moduleInfo.Packages = new List<ProtobuildPackageRef>();

                foreach (var package in moduleRef.Packages)
                {
                    var packageInfo = new ProtobuildPackageRef();
                    packageInfo.Uri = package.Uri;
                    packageInfo.GitRef = package.GitRef;
                    packageInfo.Folder = package.Folder;
                    moduleInfo.Packages.Add(packageInfo);
                }

			    moduleInfo.LoadedSubmodules = new List<ProtobuildModuleInfo> ();

			    foreach (var submodule in moduleRef.GetSubmodules ()) {
			        moduleInfo.LoadedSubmodules.Add (LoadModule (submodule.Path));
			    }

				return moduleInfo;
			}

		    public string GetHostPlatform ()
		    {
		        var hostPlatformDetectorType = internalAssembly.GetType ("Protobuild.HostPlatformDetector");
		        dynamic hostPlatformDetector = Activator.CreateInstance (hostPlatformDetectorType);
		        string platform = hostPlatformDetector.DetectPlatform ();
		        return platform;
		    }

            public void SaveModule(ProtobuildModuleInfo moduleInfo)
            {
                var moduleInfoType = internalAssembly.GetType("Protobuild.ModuleInfo");
                var moduleInfoLoad = moduleInfoType.GetMethod("Load");
                dynamic moduleRef = moduleInfoLoad.Invoke(null, new object[] { Path.Combine(moduleInfo.Path, "Build", "Module.xml") });

                moduleRef.Name = moduleInfo.Name;
                moduleRef.DefaultLinuxPlatforms = moduleInfo.DefaultLinuxPlatforms;
                moduleRef.DefaultWindowsPlatforms = moduleInfo.DefaultWindowsPlatforms;
                moduleRef.DefaultMacOSPlatforms = moduleInfo.DefaultMacOSPlatforms;
                moduleRef.DefaultStartupProject = moduleInfo.DefaultStartupProject;
                moduleRef.DisableSynchronisation = moduleInfo.DisableSynchronisation;
                moduleRef.GenerateNuGetRepositories = moduleInfo.GenerateNuGetRepositories;
                moduleRef.SupportedPlatforms = moduleInfo.SupportedPlatforms;
                moduleRef.DefaultAction = moduleInfo.DefaultAction;

                // TODO: Save package references (we need to dynamically create PackageRefs from the Protobuild module).

                moduleRef.Save(Path.Combine(moduleInfo.Path, "Build", "Module.xml"));
            }
		}
    }
}

