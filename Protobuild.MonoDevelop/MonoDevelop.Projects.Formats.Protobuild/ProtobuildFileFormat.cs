//
// ProtobuildFileFormat.cs
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
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Projects.Formats.Protobuild
{
	public class ProtobuildFileFormat : IFileFormat
    {
		public FilePath GetValidFormatName (object obj, FilePath fileName)
		{
			return fileName;
		}

		public bool CanReadFile (FilePath file, Type expectedObjectType)
		{
			if (expectedObjectType.IsAssignableFrom (typeof(Solution)))
			{
				if (file.FileName == "Protobuild.exe")
				{
					return true;
				}
			}

			return false;
		}

		public bool CanWriteFile (object obj)
		{
		    return obj is ProtobuildModule || obj is ProtobuildDefinition;
		}

		public void ConvertToFormat (object obj)
		{
		}

		public void WriteFile (FilePath file, object obj, IProgressMonitor monitor)
		{
		    if (obj is ProtobuildModule) {
		        ((ProtobuildModule)obj).SaveModule(file, monitor);
            }

            if (obj is ProtobuildDefinition)
            {
                // TODO
                //((ProtobuildDefinition)obj).SaveDefinition(file, monitor);
            }
		}

		public object ReadFile (FilePath file, Type expectedType, IProgressMonitor monitor)
		{
			if (file == null || monitor == null)
				return null;

			ProtobuildModule module;
			try {
				ProjectExtensionUtil.BeginLoadOperation();
				module = new ProtobuildModule();
				monitor.BeginTask(GettextCatalog.GetString("Loading Protobuild module: {0}", file), 1);
				var projectLoadMonitor = monitor as IProjectLoadProgressMonitor;
				if (projectLoadMonitor != null)
					projectLoadMonitor.CurrentSolution = module;
				LoadModule(module, file, monitor);
			} catch (Exception ex) {
				monitor.ReportError(GettextCatalog.GetString("Could not load Protobuild module: {0}", file), ex);
				throw;
			} finally {
				ProjectExtensionUtil.EndLoadOperation();
				monitor.EndTask();
			}

			return module;
		}

		public List<FilePath> GetItemFiles (object obj)
		{
			return new List<FilePath> ();
		}

		public IEnumerable<string> GetCompatibilityWarnings (object obj)
		{
			throw new NotImplementedException ();
		}

		public bool SupportsFramework (TargetFramework framework)
		{
			return true;
		}

		public bool SupportsMixedFormats {
			get {
				return false;
			}
		}

		void LoadModule (ProtobuildModule module, FilePath file, IProgressMonitor monitor)
		{
			module.Load(file, monitor);
		}
    }
}

