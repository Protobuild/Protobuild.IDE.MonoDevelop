// ProtobuildDefinitionLoader.cs
//
// Copyright (c) 2015 June Rhodes
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
using System.Xml;

namespace MonoDevelop.Projects.Formats.Protobuild
{
	public static class ProtobuildDefinitionLoader
	{
		public static IProtobuildDefinition CreateDefinition(ProtobuildModuleInfo modulel, ProtobuildDefinitionInfo definitionl, IProtobuildModule moduleObj)
		{
			var document = new XmlDocument ();
			document.Load(definitionl.DefinitionPath);

			switch (definitionl.Type) {
			case "External":
				return new ProtobuildExternalDefinition(modulel, definitionl, document, moduleObj);
			case "Content":
				return new ProtobuildContentDefinition(modulel, definitionl, document, moduleObj);
			default:
				return new ProtobuildStandardDefinition(modulel, definitionl, document, moduleObj);
			}
		}
	}
}

