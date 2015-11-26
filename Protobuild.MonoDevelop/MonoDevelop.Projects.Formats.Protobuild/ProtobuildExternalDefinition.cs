using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects.Formats.Protobuild
{
	public class ProtobuildExternalDefinition : ProtobuildDefinition
	{
		internal ProtobuildExternalDefinition (ProtobuildModuleInfo modulel, ProtobuildDefinitionInfo definitionl, XmlDocument document, IProtobuildModule moduleObj)
			: base(modulel, definitionl, document, moduleObj) {
			Initialize(this);
		}
    }
}
