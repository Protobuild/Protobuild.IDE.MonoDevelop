using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Collections;

namespace MonoDevelop.Projects.Formats.Protobuild
{
	public class ProtobuildPackages : WorkspaceObject, IEnumerable<ProtobuildPackage>
    {
		private IProtobuildModule _module;

		private List<ProtobuildPackage> _packages;

		public ProtobuildPackages(IProtobuildModule module) {
			_packages = new List<ProtobuildPackage>();
			_module = module;
		}

		public void Add(ProtobuildPackage package) {
			_packages.Add(package);
		}

		public IEnumerator<ProtobuildPackage> GetEnumerator() {
			return _packages.AsReadOnly().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return _packages.AsReadOnly().GetEnumerator();
		}

		protected override string OnGetBaseDirectory ()
		{
			return _module.RootFolder.BaseDirectory;
		}

		protected override string OnGetName ()
		{
			return "Packages";
		}

		protected override string OnGetItemDirectory ()
		{
			return _module.RootFolder.ItemDirectory;
		}

		protected override IEnumerable<WorkspaceObject> OnGetChildren ()
		{
			return _packages;
		}

		public int Count {
			get { return _packages.Count; } 
		}
    }
}
