using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Formats.Protobuild
{
	public class ProtobuildServiceFilter : ProjectItem, IProtobuildExternalRefOrServiceFilter
	{
		public string Name { get; set; }

		public ItemCollection<ProtobuildExternalRef> Items { get; private set; }

		public ProtobuildServiceFilter()
		{
			this.Items = new ItemCollection<ProtobuildExternalRef>();
		}
    }
}
