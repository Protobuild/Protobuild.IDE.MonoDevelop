using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Projects.Protobuild.Templates;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using Newtonsoft.Json;
using Xwt.Drawing;
using System.ComponentModel;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide;

#if MONODEVELOP_5

namespace MonoDevelop.Protobuild
{
    class NewProtobuildModuleDialog : INewProtobuildModuleDialogController
	{
		string chooseTemplateBannerText =  GettextCatalog.GetString ("Choose a template for your new module");
		string configureYourModuleBannerText = GettextCatalog.GetString ("Configure your new module");

		const string UseGitPropertyName = "Dialogs.NewProjectDialog.UseGit";
		const string CreateGitIgnoreFilePropertyName = "Dialogs.NewProjectDialog.CreateGitIgnoreFile";
		const string CreateProjectSubDirectoryPropertyName = "MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.AutoCreateProjectSubdir";
		const string CreateProjectSubDirectoryInExistingSolutionPropertyName = "Dialogs.NewProjectDialog.AutoCreateProjectSubdirInExistingSolution";
		const string LastSelectedCategoryPropertyName = "Dialogs.NewProjectDialog.LastSelectedCategoryPath";
		const string SelectedLanguagePropertyName = "Dialogs.NewProjectDialog.SelectedLanguage";

		List<TemplateCategory> templateCategories;
		INewProtobuildModuleDialogBackend dialog;
		FinalProtobuildModuleConfigurationPage finalConfigurationPage;
        ProtobuildModuleCreationPage creationPage;
		TemplateWizardProvider wizardProvider;
		IVersionControlProjectTemplateHandler versionControlHandler;
		TemplateImageProvider imageProvider = new TemplateImageProvider ();

		NewProjectConfiguration projectConfiguration = new NewProjectConfiguration () {
			CreateProjectDirectoryInsideSolutionDirectory = true
		};

		public bool OpenSolution { get; set; }
		public bool IsNewItemCreated { get; private set; }

		public IWorkspaceFileObject NewItem {
			get { 
				if (processedTemplate != null) {
					return processedTemplate.WorkspaceItems.FirstOrDefault ();
				}
				return null;
			}
		}

		public SolutionFolder ParentFolder { get; set; }
		public string BasePath { get; set; }
		public string SelectedTemplateId { get; set; }

		string DefaultSelectedCategoryPath {
			get {
				return PropertyService.Get<string> (LastSelectedCategoryPropertyName, null);
			}
			set {
				PropertyService.Set (LastSelectedCategoryPropertyName, value);
			}
		}

		public bool IsNewSolution {
			get { return projectConfiguration.CreateSolution; }
		}

		ProcessedTemplateResult processedTemplate;
		List <SolutionItem> currentEntries;
		bool disposeNewItem = true;

		public NewProtobuildModuleDialog ()
		{
			IsFirstPage = true;
			LoadTemplateCategories ();
			GetVersionControlHandler ();
		}

		public bool Show ()
		{
			projectConfiguration.CreateSolution = ParentFolder == null;
			SetDefaultSettings ();
			SelectDefaultTemplate ();

            CreateFinalConfigurationPage();
            CreateCreationPage();
			CreateWizardProvider ();

			dialog = CreateNewProjectDialog ();
			dialog.RegisterController (this);

			dialog.ShowDialog ();

			if (disposeNewItem)
				DisposeExistingNewItems ();

			return IsNewItemCreated;
		}

        private void CreateCreationPage ()
        {
            creationPage = new ProtobuildModuleCreationPage();
        }

        void GetVersionControlHandler ()
		{
			versionControlHandler = AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/VersionControlProjectTemplateHandler", typeof(IVersionControlProjectTemplateHandler), true)
				.Select (extensionObject => (IVersionControlProjectTemplateHandler)extensionObject)
				.FirstOrDefault ();
		}

		void SetDefaultSettings ()
		{
			SetDefaultLocation ();
			SetDefaultGitSettings ();
			SelectedLanguage = PropertyService.Get (SelectedLanguagePropertyName, "C#");
			projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory = GetDefaultCreateProjectDirectorySetting ();
		}

		bool GetDefaultCreateProjectDirectorySetting ()
		{
			if (IsNewSolution) {
				return PropertyService.Get (CreateProjectSubDirectoryPropertyName, true);
			}
			return PropertyService.Get (CreateProjectSubDirectoryInExistingSolutionPropertyName, true);
		}

		void UpdateDefaultSettings ()
		{
			UpdateDefaultGitSettings ();
			UpdateDefaultCreateProjectDirectorySetting ();
			PropertyService.Set (SelectedLanguagePropertyName, SelectedLanguage);
			DefaultSelectedCategoryPath = GetSelectedCategoryPath ();
		}

		string GetSelectedCategoryPath ()
		{
			foreach (TemplateCategory topLevelCategory in templateCategories) {
				foreach (TemplateCategory secondLevelCategory in topLevelCategory.Categories) {
					foreach (TemplateCategory thirdLevelCategory in secondLevelCategory.Categories) {
						SolutionTemplate matchedTemplate = thirdLevelCategory
							.Templates
							.FirstOrDefault (template => template == SelectedTemplate);
						if (matchedTemplate != null) {
							return String.Format ("{0}/{1}", topLevelCategory.Id, secondLevelCategory.Id);
						}
					}
				}
			}

			return null;
		}

		void UpdateDefaultCreateProjectDirectorySetting ()
		{
			if (IsNewSolution) {
				PropertyService.Set (CreateProjectSubDirectoryPropertyName, projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory);
			} else {
				PropertyService.Set (CreateProjectSubDirectoryInExistingSolutionPropertyName, projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory);
			}
		}

		void SetDefaultLocation ()
		{
			if (BasePath == null)
				BasePath = IdeApp.ProjectOperations.ProjectsDefaultPath;

			projectConfiguration.Location = FileService.ResolveFullPath (BasePath);
		}

		void SetDefaultGitSettings ()
		{
			projectConfiguration.UseGit = PropertyService.Get (UseGitPropertyName, false);
			projectConfiguration.CreateGitIgnoreFile = PropertyService.Get (CreateGitIgnoreFilePropertyName, true);
		}

		void UpdateDefaultGitSettings ()
		{
			PropertyService.Set (UseGitPropertyName, projectConfiguration.UseGit);
			PropertyService.Set (CreateGitIgnoreFilePropertyName, projectConfiguration.CreateGitIgnoreFile);
		}

        INewProtobuildModuleDialogBackend CreateNewProjectDialog()
		{
			return new GtkNewProtobuildModuleDialogBackend ();
		}

		void CreateFinalConfigurationPage ()
		{
			finalConfigurationPage = new FinalProtobuildModuleConfigurationPage (projectConfiguration);
			finalConfigurationPage.ParentFolder = ParentFolder;
			finalConfigurationPage.IsUseGitEnabled = IsNewSolution && (versionControlHandler != null);
			finalConfigurationPage.IsValidChanged += (sender, e) => {
				dialog.CanMoveToNextPage = finalConfigurationPage.IsValid;
			};
		}

		void CreateWizardProvider ()
		{
			wizardProvider = new TemplateWizardProvider ();
			wizardProvider.CanMoveToNextPageChanged += (sender, e) => {
				dialog.CanMoveToNextPage = wizardProvider.CanMoveToNextPage;
			};
		}

		public IEnumerable<TemplateCategory> TemplateCategories {
			get { return templateCategories; }
		}

		public TemplateCategory SelectedSecondLevelCategory { get; private set; }
		public SolutionTemplate SelectedTemplate { get; set; }
		public string SelectedLanguage { get; set; }

		public FinalProtobuildModuleConfigurationPage FinalConfiguration {
			get { return finalConfigurationPage; }
		}

        public List<IProtobuildModuleTemplate> OnlineProtobuildModuleTemplates = new List<IProtobuildModuleTemplate> ();

        public string LastSearch { get; set; }

		void LoadTemplateCategories ()
        {
            // Load all builtin project templates.
		    var builtins = System.Reflection.Assembly.GetExecutingAssembly ().GetTypes ()
		        .Where (x => typeof (IProtobuildModuleTemplate).IsAssignableFrom (x))
		        .Where (x => x.GetCustomAttributes (typeof (BuiltinProtobuildModuleTemplateAttribute), false).Length > 0)
		        .Select (Activator.CreateInstance)
		        .OfType<IProtobuildModuleTemplate> ();

            var featuredCategory = new TemplateCategory("featured", "Featured", null);
            var generalCategory = new TemplateCategory("general", "General", null);
            var onlineCategory = new TemplateCategory("online", "Online", null);
            var miscCategory = new TemplateCategory("misc", "Miscellanous", null);

		    var protobuildCategory = new TemplateCategory ("protobuild", "Protobuild", "md-platform-other");
            protobuildCategory.AddCategory(featuredCategory);
            protobuildCategory.AddCategory(onlineCategory);
            protobuildCategory.AddCategory(generalCategory);
            protobuildCategory.AddCategory(miscCategory);

            var featuredGeneralCategory = new TemplateCategory("featured-general", "Featured", "md-platform-other");
            featuredCategory.AddCategory(featuredGeneralCategory);
            var generalGeneralCategory = new TemplateCategory("general-general", "General", "md-platform-other");
            generalCategory.AddCategory(generalGeneralCategory);

		    string message;
		    if (string.IsNullOrEmpty (LastSearch)) {
		        message = "Use the search in the top right!";
		    }
		    else {
		        message = "No Results for '" + LastSearch + "'";
		    }

            var onlineResultsCategory = new TemplateCategory("online-general", "Results for '" + LastSearch + "'", "md-platform-other");
            var onlineNoResultsCategory = new TemplateCategory("online-general", message, "md-platform-other");
            var miscGeneralCategory = new TemplateCategory("misc-general", "Miscellanous", "md-platform-other");
            miscCategory.AddCategory(miscGeneralCategory);

		    var online = new List<ProtobuildSolutionTemplate> ();

            foreach (var builtin in builtins.Concat(OnlineProtobuildModuleTemplates))
            {
                var project = new ProtobuildSolutionTemplate(builtin.Id, builtin.Name, builtin.IconId)
                {
                    Description = builtin.Description,
                    GroupId = builtin.GroupId,
                    ImageId = builtin.ImageId
                };

		        if (builtin.IsFeatured) {
                    featuredGeneralCategory.AddTemplate(project);
                }

                if (builtin.IsOnline)
                {
                    onlineResultsCategory.AddTemplate(project);
                    online.Add(project);
                }

                if (builtin.IsGeneral)
                {
                    generalGeneralCategory.AddTemplate(project);
                }

                if (builtin.IsMisc)
                {
                    miscGeneralCategory.AddTemplate(project);
                }
            }

            onlineCategory.AddCategory(online.Count == 0 ? onlineNoResultsCategory : onlineResultsCategory);

            var selected = online.FirstOrDefault();
            if (selected != null)
            {
                this.SelectedTemplate = selected;
            }

		    if (this.SelectedSecondLevelCategory != null) {
		        this.SelectedSecondLevelCategory = onlineCategory;
		    }

		    templateCategories = new List<TemplateCategory>();
		    templateCategories.Add(protobuildCategory);
		}

		void SelectDefaultTemplate ()
		{
			if (SelectedTemplateId != null) {
				SelectTemplate (SelectedTemplateId);
			} else if (DefaultSelectedCategoryPath != null) {
				SelectFirstTemplateInCategory (DefaultSelectedCategoryPath);
			}

			if (SelectedSecondLevelCategory == null) {
				SelectFirstAvailableTemplate ();
			}
		}

		void SelectTemplate (string templateId)
		{
			SelectTemplate (template => template.Id == templateId);
		}

		void SelectFirstAvailableTemplate ()
		{
			SelectTemplate (template => true);
		}

		void SelectFirstTemplateInCategory (string categoryPath)
		{
			List<string> parts = new TemplateCategoryPath (categoryPath).GetParts ().ToList ();
			if (parts.Count < 2) {
				return;
			}

			string topLevelCategoryId = parts [0];
			string secondLevelCategoryId = parts [1];
			SelectTemplate (
				template => true,
				category => category.Id == topLevelCategoryId,
				category => category.Id == secondLevelCategoryId);
		}

		void SelectTemplate (Func<SolutionTemplate, bool> isTemplateMatch)
		{
			SelectTemplate (isTemplateMatch, category => true, category => true);
		}

		void SelectTemplate (
			Func<SolutionTemplate, bool> isTemplateMatch,
			Func<TemplateCategory, bool> isTopLevelCategoryMatch,
			Func<TemplateCategory, bool> isSecondLevelCategoryMatch)
		{
			foreach (TemplateCategory topLevelCategory in templateCategories.Where (isTopLevelCategoryMatch)) {
				foreach (TemplateCategory secondLevelCategory in topLevelCategory.Categories.Where (isSecondLevelCategoryMatch)) {
					foreach (TemplateCategory thirdLevelCategory in secondLevelCategory.Categories) {
						SolutionTemplate matchedTemplate = thirdLevelCategory
							.Templates
							.FirstOrDefault (isTemplateMatch);
						if (matchedTemplate != null) {
							SelectedSecondLevelCategory = secondLevelCategory;
							SelectedTemplate = matchedTemplate;
							return;
						}
					}
				}
			}
		}

		public ProtobuildSolutionTemplate GetSelectedTemplateForSelectedLanguage ()
		{
			if (SelectedTemplate != null) {
				SolutionTemplate languageTemplate = SelectedTemplate.GetTemplate (SelectedLanguage);
				if (languageTemplate != null) {
					return (ProtobuildSolutionTemplate)languageTemplate;
				}
			}

            return (ProtobuildSolutionTemplate)SelectedTemplate;
		}

		public string BannerText {
			get {
			    if (IsCreationPage) {
			        return "Creating your new module...";
			    } else if (IsLastPage) {
					return GetFinalConfigurationPageBannerText ();
				} else if (IsWizardPage) {
					return wizardProvider.CurrentWizardPage.Title;
				}
				return chooseTemplateBannerText;
			}
		}

		string GetFinalConfigurationPageBannerText ()
		{
		    return configureYourModuleBannerText;
		}

		public bool CanMoveToNextPage {
			get {
			    if (IsCreationPage) {
			        return false;
			    }
				if (IsLastPage) {
					return finalConfigurationPage.IsValid;
				} else if (IsWizardPage) {
					return wizardProvider.CanMoveToNextPage;
				}
				return (SelectedTemplate != null);
			}
		}

		public bool CanMoveToPreviousPage {
			get { return !IsCreationPage && !IsFirstPage; }
		}

		public string NextButtonText {
			get {
				if (IsLastPage) {
					return GettextCatalog.GetString ("Create");
				}
				return GettextCatalog.GetString ("Next");
			}
		}

		public bool IsFirstPage { get; private set; }
		public bool IsLastPage { get; private set; }

		public bool IsWizardPage {
			get { return wizardProvider.HasWizard && !IsFirstPage && !IsLastPage; }
		}

		public void MoveToNextPage ()
		{
		    if (IsFirstPage) {
		        IsFirstPage = false;

		        FinalConfiguration.Template = GetSelectedTemplateForSelectedLanguage ();
		        if (wizardProvider.MoveToFirstPage (FinalConfiguration.Template, finalConfigurationPage.Parameters)) {
		            return;
                }

                IsLastPage = true;
		    } else if (IsLastPage) {
		        IsLastPage = false;
		        IsCreationPage = true;
                wizardProvider.MoveToNextPage();
		    } else if (wizardProvider.MoveToNextPage ()) {
				return;
            }
		}

        public bool IsCreationPage { get; set; }

        public void MoveToPreviousPage ()
		{
			if (IsWizardPage) {
				if (wizardProvider.MoveToPreviousPage ()) {
					return;
				}
			} else if (IsLastPage && wizardProvider.HasWizard) {
				IsLastPage = false;
				return;
			}

			IsFirstPage = true;
			IsLastPage = false;
		}

        private void BackgroundModuleCreation(object o)
        {
            var creationParameters = (NewProjectConfiguration)o;

			var client = new WebClient();

            const string url = "https://github.com/hach-que/Protobuild/raw/master/Protobuild.exe";
            EmitCreationLog (
                "Downloading Protobuild.exe from " + url + "...");

            var protobuildPath = Path.Combine(creationParameters.SolutionLocation, "Protobuild.exe");
			var baseTempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var downloadPath = Path.Combine(baseTempPath, "Protobuild.part");
			var cachedPath = Path.Combine(baseTempPath, "Protobuild.cache");

			try
			{
				client.DownloadFile(url, downloadPath);
				EmitCreationLog ("Download Complete.");

				File.Copy(downloadPath, cachedPath, true);
			}
			catch (WebException ex)
			{
				if (File.Exists(cachedPath))
				{
					// Cache file already exists; just use that version.
				}
				else
				{
					EmitCreationLog("Failed to download Protobuild, and no version has been cached offline.");
					EmitCreationLog("Connect to the internet to perform a first-time download of Protobuild.");
					return;
				}
			}

			EmitCreationLog("Copying Protobuild.exe to new module...");
			File.Copy(cachedPath, protobuildPath, true);

			// TODO: SSL doesn't work here on Linux, presumably because Mono
			// isn't compatible with the CloudFlare SSL layer.
            var templateUrl = "http://protobuild.org/" + SelectedTemplate.Id;

            EmitCreationLog("Running Protobuild.exe --start " + templateUrl + " " + creationParameters.SolutionName);

            try
            {
                var chmodStartInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = "a+x Protobuild.exe",
                    WorkingDirectory = creationParameters.SolutionLocation,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(chmodStartInfo);
            }
            catch
            {
            }

            if (File.Exists(protobuildPath))
            {
                var pi = new ProcessStartInfo
                {
                    FileName = protobuildPath,
                    Arguments = "--start " + templateUrl + " " + creationParameters.SolutionName,
                    WorkingDirectory = creationParameters.SolutionLocation,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var p = new Process { StartInfo = pi };
                p.OutputDataReceived += (sender, eventArgs) =>
                {
                    if (!string.IsNullOrEmpty(eventArgs.Data))
                    {
                        EmitCreationLog(eventArgs.Data);
                    }
                };
                p.ErrorDataReceived += (sender, eventArgs) =>
                {
                    if (!string.IsNullOrEmpty(eventArgs.Data))
                    {
                        EmitCreationLog(eventArgs.Data);
                    }
                };

				var started = false;
				while (!started)
				{
					try
					{
	                	p.Start();
						started = true;
					}
					catch (Win32Exception ex)
					{
						if (ex.Message.Contains("Cannot find the specified file"))
						{
							if (File.Exists(protobuildPath))
							{
								// Some weird race condition that occurs within Mono... :/
								Thread.Sleep(100);
							}
							else
							{
								throw;
							}
						}
						else
						{
							throw;
						}
					}
				}

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();

                if (p.ExitCode == 0)
                {
                    EmitCreationLog("Protobuild created the module successfully.");
                    FinishCreation(creationParameters);
                }
                else
                {
                    EmitCreationLog("Protobuild failed to create the new module (press Cancel to close this dialog).");
                }
            }
        }

        private void FinishCreation (NewProjectConfiguration creationParameters)
        {
			Gtk.Application.Invoke ((sender, args) => {
				IAsyncOperation asyncOperation = IdeApp.Workspace.OpenWorkspaceItem(Path.Combine(creationParameters.SolutionLocation, "Protobuild.exe"));
				asyncOperation.Completed += delegate
				{
					if (asyncOperation.Success)
					{
					}
				};

                dialog.CloseDialog ();
                wizardProvider.Dispose ();
                imageProvider.Dispose ();
            });
        }

        private void EmitCreationLog (string message)
        {
            Gtk.Application.Invoke ((sender, args) => {
                dialog.EmitCreationLog (message);
            });
        }

        public bool ValidateAndStartCreation()
		{
			if (!ValidateProject ())
				return false;

            var thread = new Thread (BackgroundModuleCreation);
            thread.IsBackground = true;
            thread.Start (projectConfiguration);

            return true;
		}

        public WizardPage CurrentWizardPage {
			get {
				if (IsFirstPage || IsLastPage) {
					return null;
				}
				return wizardProvider.CurrentWizardPage;
			}
		}

        bool ValidateProject ()
		{
			if (!projectConfiguration.IsValid () || projectConfiguration.SolutionName.Contains (' ')) {
				MessageService.ShowError (GettextCatalog.GetString ("Illegal project name.\nOnly use letters, digits, '.' or '_'."));
				return false;
			}

			if (ParentFolder != null && ParentFolder.ParentSolution.FindProjectByName (projectConfiguration.ProjectName) != null) {
				MessageService.ShowError (GettextCatalog.GetString ("A Project with that name is already in your Project Space"));
				return false;
			}

			ProcessedTemplateResult result = null;

			try {
				if (Directory.Exists (projectConfiguration.ProjectLocation)) {
					var question = GettextCatalog.GetString ("Directory {0} already exists.\nDo you want to continue creating the project?", projectConfiguration.ProjectLocation);
					var btn = MessageService.AskQuestion (question, AlertButton.No, AlertButton.Yes);
					if (btn != AlertButton.Yes)
						return false;
				}

				Directory.CreateDirectory (projectConfiguration.SolutionLocation);
			} catch (IOException) {
				MessageService.ShowError (GettextCatalog.GetString ("Could not create directory {0}. File already exists.", projectConfiguration.ProjectLocation));
				return false;
			} catch (UnauthorizedAccessException) {
				MessageService.ShowError (GettextCatalog.GetString ("You do not have permission to create to {0}", projectConfiguration.ProjectLocation));
				return false;
			}

			DisposeExistingNewItems ();

		    return true;
		}

		void DisposeExistingNewItems ()
		{
			if (processedTemplate != null) {
				foreach (IDisposable item in processedTemplate.WorkspaceItems) {
					item.Dispose ();
				}
			}
		}

        public Image GetImage (SolutionTemplate template)
		{
			return imageProvider.GetImage (template);
		}

        public void SearchRequested (string text)
        {
            var thread = new Thread (UpdateSearchResultsBackground);
            thread.IsBackground = true;
            thread.Start (text);
        }

        private object lockObject = new object ();
        private int requestNumber = -1;
        private int updatedRequest = -2;

        private void UpdateSearchResultsBackground (object o)
        {
            var requestingResultsFor = (string)o;
            var webClient = new WebClient ();

            int thisRequest = -1;
            lock (lockObject) {
                thisRequest = requestNumber + 1;
                requestNumber++;
            }

            try {
                var data = webClient.DownloadString ("http://api.protobuild-index.appspot.com/search/" +
                                                     HttpUtility.UrlEncode (requestingResultsFor) + "?template=true");
                var results = JsonConvert.DeserializeObject<dynamic> (data);
                if (results.error.Value) {
                    return;
                }

                lock (lockObject) {
                    if (thisRequest < updatedRequest)
                    {
                        return;
                    }

                    OnlineProtobuildModuleTemplates.Clear ();

                    foreach (var result in results.results) {
                        var ownerName = result.ownerName;
                        var description = result.description;
                        var id = result.id;
                        var name = result.name;

                        var path = ownerName + "/" + name;

                        OnlineProtobuildModuleTemplates.Add (new OnlineProtobuildModuleTemplate ((string) path,
                            (string) description));
                    }

                    LastSearch = requestingResultsFor;
                    LoadTemplateCategories();

                    updatedRequest = thisRequest;
                }

                Gtk.Application.Invoke ((sender, args) => {
                    dialog.RegisterController (this);
                });
            }
            catch (WebException ex) {
                // TODO report error in UI
            }
            finally {
            }
        }
	}
}

#endif