using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

namespace MonoDevelop.AddinMaker
{
	class AddinProject : DotNetProject
	{
		public AddinProject ()
		{
			Init ();
		}

		public AddinProject (string languageName) : base (languageName)
		{
			Init ();
		}

		public AddinProject (string lang, ProjectCreateInformation info, XmlElement options)
			: base (lang, info, options)
		{
			Init ();
		}

		void Init ()
		{
			AddinReferences = new AddinReferenceCollection (this);
		}

		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var cfg = new AddinProjectConfiguration (name);
			cfg.CopyFrom (base.CreateConfiguration (name));
			return cfg;
		}

		protected override void OnItemsAdded (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsAdded (objs);

			var addinRefs = objs.OfType<AddinReference> ().ToList ();
			if (addinRefs.Count > 0) {
				AddinReferences.AddRange (addinRefs);
				var args = new AddinReferenceEventArgs ();
				foreach (var item in addinRefs) {
					item.OwnerProject = this;
					args.AddInfo (this, item);
				}
				var evt = AddinReferenceAdded;
				if (evt != null)
					evt (this, args);
			}
		}

		protected override void OnItemsRemoved (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsRemoved (objs);

			var addinRefs = objs.OfType<AddinReference> ().ToList ();
			if (addinRefs.Count > 0) {
				AddinReferences.RemoveRange (addinRefs);
				var args = new AddinReferenceEventArgs ();
				foreach (var item in addinRefs) {
					args.AddInfo (this, item);
				}
				var evt = AddinReferenceRemoved;
				if (evt != null)
					evt (this, args);
			}
		}

		public AddinReferenceCollection AddinReferences { get; private set; }

		public event EventHandler<AddinReferenceEventArgs> AddinReferenceAdded;
		public event EventHandler<AddinReferenceEventArgs> AddinReferenceRemoved;

		protected override ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration)
		{
			var cmd = (DotNetExecutionCommand) base.CreateExecutionCommand (configSel, configuration);
			cmd.Command = Assembly.GetEntryAssembly ().Location;
			cmd.Arguments = "--no-redirect";
			cmd.EnvironmentVariables["MONODEVELOP_DEV_ADDINS"] = GetOutputFileName (configSel).ParentDirectory;
			cmd.EnvironmentVariables ["MONODEVELOP_CONSOLE_LOG_LEVEL"] = "All";
			return cmd;
		}

		protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return true;
		}

		public override bool IsLibraryBasedProjectType {
			get { return true; }
		}
	}

	class AddinReferenceEventArgs : EventArgsChain<AddinReferenceEventInfo>
	{
		public void AddInfo (AddinProject project, AddinReference reference)
		{
			Add (new AddinReferenceEventInfo (project, reference));
		}
	}

	class AddinReferenceEventInfo
	{
		public AddinReference Reference { get; private set; }
		public AddinProject Project { get; private set; }

		public AddinReferenceEventInfo (AddinProject project, AddinReference reference)
		{
			this.Reference = reference;
			this.Project = project;
		}
	}
}
