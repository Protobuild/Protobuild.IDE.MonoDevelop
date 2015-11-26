using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Formats.Protobuild;

namespace MonoDevelop.Ide
{
    public abstract class ProtobuildEditorHost<T, T2> : IProtobuildEditorHost where T : IOpenedFileList<T2>
    {
        protected Dictionary<ProtobuildStandardDefinition, T> openedFiles;

		private Dictionary<ProtobuildStandardDefinition, DateTime> lastModified;

		protected Dictionary<ProtobuildStandardDefinition, List<IDisplayBinding>> registeredBindings;

        protected ProtobuildEditorHost ()
        {
			openedFiles = new Dictionary<ProtobuildStandardDefinition, T> ();
			lastModified = new Dictionary<ProtobuildStandardDefinition, DateTime> ();
			registeredBindings = new Dictionary<ProtobuildStandardDefinition, List<IDisplayBinding>> ();
        }

        public void Reload(object whatChanged)
        {
            if (whatChanged is ProtobuildModule)
            {
				foreach (var definition in ((ProtobuildModule)whatChanged).Definitions.OfType<ProtobuildStandardDefinition>())
                {
                    ScanOutputForChanges(definition);
                }
            }
			else if (whatChanged is ProtobuildStandardDefinition)
            {
				ScanOutputForChanges((ProtobuildStandardDefinition)whatChanged);
            }
        }

		private void ScanOutputForChanges(ProtobuildStandardDefinition definition)
        {
            if (definition.Type != "IDEEditor")
            {
                return;
            }

            var platform = "Windows";
            if (Path.DirectorySeparatorChar == '/') {
                platform = "Linux";
            }

            // TODO: Calculate output path.
            var outputDir = Path.Combine(definition.ProjectDirectory, @"bin/" + platform + "/AnyCPU/Debug");
            var outputPath = Path.Combine(outputDir, definition.Name + ".dll");
            var modificationDate = new FileInfo(outputPath).LastWriteTimeUtc;
            if (lastModified.ContainsKey(definition))
            {
                if (openedFiles.ContainsKey(definition))
                {
                    if (modificationDate <= lastModified[definition])
                    {
                        Console.WriteLine(definition.Name + " not modified on disk since last check");
                        return;
                    }
                }
            }
            lastModified[definition] = modificationDate;

            Console.WriteLine(definition.Name + " detected as IDE editor, starting external GTK plug process");

            if (!openedFiles.ContainsKey(definition)) {
                openedFiles[definition] = StartInfo(definition, outputPath);
            }

            if (registeredBindings.ContainsKey(definition))
            {
                foreach (var binding in registeredBindings[definition])
                {
                    DisplayBindingService.DeregisterRuntimeDisplayBinding(binding);
                }
                registeredBindings.Remove(definition);
            }

            var toResume = new List<T2>();

            var barrier = new Barrier(2);

            // Suspend editors on the main thread.
            GLib.Timeout.Add(1, new GLib.TimeoutHandler(() => {
                foreach (var openedFile in openedFiles[definition].OpenedFiles)
                {
                    if (ShouldReload(openedFile)) {
                        SuspendEditor(definition, openedFile);
                        toResume.Add(openedFile);
                    }
                }
                barrier.SignalAndWait();
                return false;
            }));

            barrier.SignalAndWait();

            StopInfo (definition);

            InitializeDefinitionAndRegisterBindings(definition, openedFiles[definition]);

            // Resume editors on the main thread.
            GLib.Timeout.Add(1, new GLib.TimeoutHandler(() =>
            {
                foreach (var openedFile in toResume) {
                    ResumeEditor (definition, openedFile);
                }

                barrier.SignalAndWait();

                return false;
            }));

            barrier.SignalAndWait();
        }

		protected abstract T StartInfo(ProtobuildStandardDefinition definition, string outputPath);

        protected abstract bool ShouldReload(T2 openedFile);

		protected abstract void SuspendEditor(ProtobuildStandardDefinition definition, T2 openedFile);

		protected abstract void ResumeEditor(ProtobuildStandardDefinition definition, T2 openedFile);

		protected abstract void StopInfo (ProtobuildStandardDefinition definition);

		protected abstract void InitializeDefinitionAndRegisterBindings(ProtobuildStandardDefinition definition, T targetList);

        protected abstract void RegisterBinding (T openFileList, string ext);
    }
}
