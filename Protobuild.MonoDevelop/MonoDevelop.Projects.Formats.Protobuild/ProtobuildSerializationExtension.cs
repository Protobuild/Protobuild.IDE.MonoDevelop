// ProtobuildSerializationExtension.cs
//
// Copyright (c) 2015 james
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
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Formats.Protobuild
{
    public class ProtobuildSerializationExtension : WorkspaceObjectReader
    {
        public override bool CanRead (FilePath file, Type expectedType)
        {
            if (expectedType.IsAssignableFrom (typeof(Solution)))
            {
                if (file.FileName == "Protobuild.exe")
                {
                    return true;
                }
            }

            return false;
        }

        public override System.Threading.Tasks.Task<WorkspaceItem> LoadWorkspaceItem (ProgressMonitor monitor, string fileName)
        {
            return Task.Run (async () => {
                return (WorkspaceItem) await ReadFile (fileName, typeof(WorkspaceItem), monitor);
            });
        }

        public async Task<WorkspaceItem> ReadFile (string file, Type expectedType, ProgressMonitor monitor)
        {
            if (file == null || monitor == null)
                return null;

            ProtobuildModule module;
            try {
                //ProjectExtensionUtil.BeginLoadOperation();
                module = new ProtobuildModule();
                monitor.BeginTask(GettextCatalog.GetString("Loading Protobuild module: {0}", file), 1);
                /*var projectLoadMonitor = monitor as IProjectLoadProgressMonitor;
                if (projectLoadMonitor != null)
                    projectLoadMonitor.CurrentSolution = module;*/
                await LoadModule(module, file, monitor);
            } catch (Exception ex) {
                monitor.ReportError(GettextCatalog.GetString("Could not load Protobuild module: {0}", file), ex);
                throw;
            } finally {
                //ProjectExtensionUtil.EndLoadOperation();
                monitor.EndTask();
            }

            return module;
        }

        async Task LoadModule (ProtobuildModule module, FilePath file, ProgressMonitor monitor)
        {
            await module.Load(file, monitor);
        }
    }
}

