using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Formats.Protobuild
{
    class MarshalledProgressMonitor : MarshalByRefObject, IProgressMonitor
    {
		public event MonitorHandler CancelRequested {
			add { WrappedMonitor.CancelRequested += value; }
			remove { WrappedMonitor.CancelRequested -= value; }
		}

		public IAsyncOperation AsyncOperation {
			get { return WrappedMonitor.AsyncOperation; }
		}

		public bool IsCancelRequested {
			get { return WrappedMonitor.IsCancelRequested; }
		}
		
		public System.IO.TextWriter Log {
			get { return WrappedMonitor.Log; }
		}

		public object SyncRoot {
			get { return WrappedMonitor.SyncRoot; }
		}
		
		IProgressMonitor WrappedMonitor {
			get; set;
		}

        public MarshalledProgressMonitor(IProgressMonitor monitor)
		{
			WrappedMonitor = monitor;
		}

		public void BeginStepTask (string name, int totalWork, int stepSize)
		{
			WrappedMonitor.BeginStepTask (name, totalWork, stepSize);
		}

		public void BeginTask (string name, int totalWork)
		{
			WrappedMonitor.BeginTask (name, totalWork);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing)
				return;

			if (WrappedMonitor != null) {
				WrappedMonitor.Dispose ();
				WrappedMonitor = null;
			}
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		public void EndTask ()
		{
			WrappedMonitor.EndTask ();
		}
		
		public void ReportError (string message, Exception exception)
		{
			WrappedMonitor.ReportError (message, exception);
		}
		
		public void ReportSuccess (string message)
		{
			WrappedMonitor.ReportSuccess (message);
		}
		
		public void ReportWarning (string message)
		{
			WrappedMonitor.ReportWarning (message);
		}
		
		public void Step (int work)
		{
			WrappedMonitor.Step (work);
		}
    }
}
