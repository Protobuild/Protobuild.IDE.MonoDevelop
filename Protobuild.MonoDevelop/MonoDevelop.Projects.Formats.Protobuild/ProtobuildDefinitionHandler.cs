using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects.Formats.Protobuild
{
    public class ProtobuildDefinitionHandler : ISolutionItemHandler
    {
        private ProtobuildDefinitionInfo definition;

        public ProtobuildDefinitionHandler (ProtobuildDefinitionInfo definitionl)
        {
            definition = definitionl;
        }

        public void Dispose()
        {
        }

        public BuildResult RunTarget(IProgressMonitor monitor, string target, ConfigurationSelector configuration)
        {
            throw new NotSupportedException();
        }

        public void Save(IProgressMonitor monitor)
        {
			// TODO

			/*if (projectBuilder != null)
			{
				projectBuilder.Refresh ();
			}*/
        }

        public bool SyncFileName
        {
            get { return true; }
        }

        public string ItemId
        {
            get { return definition.Guid.ToString (); }
        }

        public void OnModified(string hint)
        {
        }

        public object GetService(Type t)
        {
            return null;
        }
    }
}
