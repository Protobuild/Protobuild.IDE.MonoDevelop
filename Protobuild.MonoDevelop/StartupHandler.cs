using MonoDevelop.Components.Commands;

namespace Protobuild.MonoDevelop
{
    public class StartupHandler : CommandHandler
    {
        protected override void Run ()
        {
            var tl = new TemplateLoader();
			tl.LoadTemplateCategories();
            tl.LoadTemplates();
        }
    }
}

