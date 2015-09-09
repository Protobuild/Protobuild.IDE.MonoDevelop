//
// ProtobuildModuleInfo.cs
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

namespace MonoDevelop.Projects.Formats.Protobuild
{
	[Serializable]
    public class ProtobuildModuleInfo
    {
		public string Name { get; set; }

		public string Path { get; set; }

		public string DefaultAction { get; set; }

		public string DefaultWindowsPlatforms { get; set; }

		public string DefaultMacOSPlatforms { get; set; }

		public string DefaultLinuxPlatforms { get; set; }

		public bool GenerateNuGetRepositories { get; set; }

		public string SupportedPlatforms { get; set; }

		public bool? DisableSynchronisation { get; set; }

		public string DefaultStartupProject { get; set; }

		public List<ProtobuildPackageRef> Packages { get; set; }

		public List<ProtobuildDefinitionInfo> LoadedDefinitions { get; set; }

	    public List<ProtobuildModuleInfo> LoadedSubmodules { get; set; }
    }
}

