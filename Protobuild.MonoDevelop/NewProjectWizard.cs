using MonoDevelop.Ide.Templates;
using System;

namespace Protobuild.MonoDevelop
{
    public class NewProjectWizard : TemplateWizard
    {
        public override string Id {
            get {
                return "ProtobuildWizard";
            }
        }

        public override WizardPage GetPage (int pageNumber)
        {
            throw new NotImplementedException();
        }
    }
}

