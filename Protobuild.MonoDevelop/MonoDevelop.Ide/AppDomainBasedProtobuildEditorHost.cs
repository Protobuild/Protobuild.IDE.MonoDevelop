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
using System.Runtime.Remoting;
using MonoDevelop.Ide.Gui;
using OpenTK.Graphics;
using OpenTK.Platform;

namespace MonoDevelop.Ide
{
	public class AppDomainBasedProtobuildEditorHost : ProtobuildEditorHost<AppDomainOpenedFileList, AppDomainOpenedFile>
	{
		public AppDomainBasedProtobuildEditorHost()
		{
			GLib.Timeout.Add(16, new GLib.TimeoutHandler(() => { this.UpdateAllDomains(); return true; }));
		}

        protected override void RegisterBinding(AppDomainOpenedFileList openFileList, string ext)
        {
            var binding = new AppDomainDisplayBinding(this, openFileList, ext);
            if (!registeredBindings.ContainsKey(openFileList.Definition))
            {
                registeredBindings[openFileList.Definition] = new List<IDisplayBinding>();
            }
            registeredBindings[openFileList.Definition].Add(binding);
            DisplayBindingService.RegisterRuntimeDisplayBinding(binding);
        }

	    protected override AppDomainOpenedFileList StartInfo (ProtobuildDefinition definition, string outputPath)
        {
            var openedFileList = new AppDomainOpenedFileList();
            openedFileList.Definition = definition;
            openedFileList.LibraryPath = outputPath;
            openedFileList.OpenedFiles = new List<AppDomainOpenedFile>();
            return openedFileList;
	    }

	    protected override bool ShouldReload (AppDomainOpenedFile openedFile)
        {
            return openedFile.AppDomain != null && openedFile.ViewContent != null &&
                   !openedFile.ViewContent.IsSuspended;
	    }

	    protected override void SuspendEditor (ProtobuildDefinition definition, AppDomainOpenedFile openedFile)
        {
            Console.WriteLine("Suspending editor for " + openedFile.FileReference + " in " + definition.Name);
            openedFile.ViewContent.Suspend();
	    }

	    protected override void ResumeEditor (ProtobuildDefinition definition, AppDomainOpenedFile openedFile)
        {
            Console.WriteLine("Resuming editor for " + openedFile.FileReference + " in " + definition.Name);
            openedFile.ViewContent.Resume();
	    }

	    protected override void StopInfo (ProtobuildDefinition definition)
	    {
	        openedFiles[definition].InfoBehaviour = null;
	        if (openedFiles[definition].InfoAppDomain != null) {
	            AppDomain.Unload (openedFiles[definition].InfoAppDomain);
	        }
	        openedFiles[definition].InfoAppDomain = null;
	    }

	    /**public override void StopEditorForFile (AppDomainOpenedFile openedFile)
        {
            openedFile.Behaviour = null;
	        if (openedFile.AppDomain != null) {
	            AppDomain.Unload (openedFile.AppDomain);
	        }
	        openedFile.AppDomain = null;
        }*/

	    protected override void InitializeDefinitionAndRegisterBindings (ProtobuildDefinition definition, AppDomainOpenedFileList targetList)
        {
            AppDomain appDomain;
            ProtobuildIDEEditorDomainBehaviour domainBehaviour;
            CreateAppDomainAndBehaviour(targetList, out appDomain, out domainBehaviour);

            targetList.InfoAppDomain = appDomain;
            targetList.InfoBehaviour = domainBehaviour;

            foreach (var ext in targetList.InfoBehaviour.GetHandledFileExtensions())
            {
                RegisterBinding(targetList, ext);
            }
        }

		private void UpdateAllDomains()
		{
		    if (openedFiles != null) {
		        foreach (var ofl in openedFiles) {
		            if (ofl.Value.OpenedFiles != null) {
		                foreach (var of in ofl.Value.OpenedFiles) {
                            if (of.Behaviour != null)
                            {
                                try
                                {
                                    of.Behaviour.Update();
                                }
                                catch (RemotingException)
                                {
                                    Console.WriteLine("Auto-reloading behaviour because of remoting exception...");
                                    of.Behaviour = null;
                                    if (of.AppDomain != null) {
                                        AppDomain.Unload (of.AppDomain);
                                    }
                                    of.AppDomain = null;
                                    of.StartAppDomain ();
                                    throw;
                                }
		                    }
		                }
		            }
		        }
		    }
		}

	    public void CreateAppDomainAndBehaviour (AppDomainOpenedFileList openedFileList, out AppDomain appDomain, out ProtobuildIDEEditorDomainBehaviour domainBehaviour)
	    {
            appDomain = AppDomain.CreateDomain(
                "IDE Editor",
                null,
                AppDomain.CurrentDomain.BaseDirectory,
                AppDomain.CurrentDomain.RelativeSearchPath,
                true);
            domainBehaviour =
                (ProtobuildIDEEditorDomainBehaviour)
                appDomain.CreateInstanceAndUnwrap(
                    typeof(ProtobuildIDEEditorDomainBehaviour).Assembly.FullName,
                    typeof(ProtobuildIDEEditorDomainBehaviour).FullName);
            domainBehaviour.Init(openedFileList.LibraryPath);
	    }
	}
}

