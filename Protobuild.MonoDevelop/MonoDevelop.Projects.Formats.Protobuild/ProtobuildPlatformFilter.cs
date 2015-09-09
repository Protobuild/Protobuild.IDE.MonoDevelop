using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Formats.Protobuild
{
	public class ProtobuildPlatformFilter : ProjectItem
    {
		public string Name { get; set; }

		public ItemCollection<IProtobuildExternalRefOrServiceFilter> Items { get; private set; }

		public ProtobuildPlatformFilter()
		{
			this.Items = new ItemCollection<IProtobuildExternalRefOrServiceFilter>();
		}
    }
}
