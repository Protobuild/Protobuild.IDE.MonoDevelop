//
// ProtobuildIDEEditorDomainBehaviour.cs
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
using System.Reflection;
using System.IO;
using System.Linq;
using OpenTK.Graphics;
using OpenTK.Platform;

namespace MonoDevelop.Ide
{
	public class ProtobuildIDEEditorDomainBehaviour : MarshalByRefObject
	{
		private Assembly targetAssembly;

		private Type editorClass;

		private object editorInstance;

		private static string _path;

		private Exception lastException;

		static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
		{
			string assemblyPath = Path.Combine(_path, new AssemblyName(args.Name).Name + ".dll");
			if (File.Exists(assemblyPath) == false) return null;
			Assembly assembly = Assembly.LoadFrom(assemblyPath);
			return assembly;
		}

		public void Init (string path)
		{
			_path = new FileInfo(path).DirectoryName;

			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);

		    if (!File.Exists (path)) {
		        return;
		    }

			targetAssembly = Assembly.LoadFrom(path);

			editorClass = targetAssembly.GetTypes().FirstOrDefault(x => x.Name == "IDEEditor");
			if (editorClass == null)
			{
				return;
			}

			editorInstance = Activator.CreateInstance(editorClass);

			var initMethod = editorClass.GetMethods().FirstOrDefault(x => x.Name == "Init");
			if (initMethod != null)
			{
				try
				{
					initMethod.Invoke(editorInstance, null);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					lock (exLock)
					{
						lastException = ex;
					}
				}
			}
		}

		public string[] GetHandledFileExtensions()
		{
		    if (editorClass == null) {
		        return new string[0];
		    }

			var getFileExtensionsMethod = editorClass.GetMethods().FirstOrDefault(x => x.Name == "GetHandledFileExtensions");
			if (getFileExtensionsMethod != null)
			{
				try
				{
					return (string[])getFileExtensionsMethod.Invoke(editorInstance, null);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					lock (exLock)
					{
						lastException = ex;
					}
				}
			}

			return new string[0];
		}

		public byte[] Suspend ()
        {
            if (editorClass == null)
            {
                return null;
            }

			var suspendMethod = editorClass.GetMethods().FirstOrDefault(x => x.Name == "Suspend");
			if (suspendMethod != null)
			{
				try
				{
					return suspendMethod.Invoke(editorInstance, new object[0]) as byte[];
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					lock (exLock)
					{
						lastException = ex;
					}
				}
			}

			return null;
		}

		public void Resume (IGraphicsContext context, IWindowInfo windowInfo, byte[] state)
        {
            if (editorClass == null)
            {
                return;
            }

			var resumeMethod = editorClass.GetMethods().FirstOrDefault(x => x.Name == "Resume");
			if (resumeMethod != null)
			{
				try
				{
					resumeMethod.Invoke(editorInstance, new object[] { context, windowInfo, state });
				}
				catch (Exception ex)
				{
					lastException = ex;
					Console.WriteLine(ex);
				}
			}
		}

        public void Resume(IntPtr windowHandle, byte[] state)
        {
            if (editorClass == null)
            {
                return;
            }

            var resumeMethod = editorClass.GetMethods().FirstOrDefault(x => x.Name == "Resume");
            if (resumeMethod != null)
            {
                try
                {
                    resumeMethod.Invoke(editorInstance, new object[] { windowHandle, state });
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.WriteLine(ex);
                }
            }
        }

		public void Load (string filePath)
        {
            if (editorClass == null)
            {
                return;
            }

			var loadMethod = editorClass.GetMethods().FirstOrDefault(x => x.Name == "Load");
			if (loadMethod != null)
			{
				try
				{
					loadMethod.Invoke(editorInstance, new object[] { filePath });
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					lock (exLock)
					{
						lastException = ex;
					}
				}
			}
		}

		public void Save (string filePath)
        {
            if (editorClass == null)
            {
                return;
            }

			var saveMethod = editorClass.GetMethods().FirstOrDefault(x => x.Name == "Save");
			if (saveMethod != null)
			{
				try
				{
					saveMethod.Invoke(editorInstance, new object[] { filePath });
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					lock (exLock)
					{
						lastException = ex;
					}
				}
			}
		}

		private object exLock = new object();

		public Exception GetAndResetLastException()
		{
			lock (exLock)
			{
				var ex = lastException;
				lastException = null;
				return ex;
			}
		}

		private MethodInfo _update;

		private bool _hasSearchedForUpdateMethod;

		public void Update()
        {
            if (editorClass == null)
            {
                return;
            }

			if (!_hasSearchedForUpdateMethod)
			{
				_update = editorClass.GetMethods().FirstOrDefault(x => x.Name == "Update");
				_hasSearchedForUpdateMethod = true;
			}

			if (_update != null)
			{
				try
				{
					_update.Invoke(editorInstance, new object[] { });
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}
		}

		public void Resize(int width, int height)
        {
            if (editorClass == null)
            {
                return;
            }

			var resize = editorClass.GetMethods().FirstOrDefault(x => x.Name == "Resize");
			if (resize != null)
			{
				try
				{
					resize.Invoke(editorInstance, new object[] { width, height });
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}
		}
	}
}

