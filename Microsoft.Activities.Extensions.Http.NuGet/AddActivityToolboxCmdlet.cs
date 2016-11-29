// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddActivityToolboxCmdlet.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Extensions.Http.NuGet
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Reflection;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.OLE.Interop;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using VSLangProj;

    using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    /// <summary>
    /// The add activity toolbox cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "ActivityToolbox")]
    public class AddActivityToolboxCmdlet : PSCmdlet
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets Activity.
        /// </summary>
        [Parameter(Mandatory = true, Position = 2, HelpMessage = "The full Activity type name")]
        public string Activity { get; set; }

        /// <summary>
        /// Gets or sets ActivityAssembly.
        /// </summary>
        [Parameter(Mandatory = true, Position = 4, 
            HelpMessage = "The activity assembly name (no extension - must be referenced)")]
        public string ActivityAssembly { get; set; }

        /// <summary>
        /// Gets or sets BitmapID.
        /// </summary>
        [Parameter(Mandatory = true, Position = 5, HelpMessage = "The resource ID of the bitmap")]
// ReSharper disable InconsistentNaming
        public string BitmapID { get; set; }
// ReSharper restore InconsistentNaming

        /// <summary>
        /// Gets or sets Category.
        /// </summary>
        [Parameter(Mandatory = true, Position = 3, HelpMessage = "The toolbox category (tab name)")]
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets DisplayName.
        /// </summary>
        [Parameter(Mandatory = false, Position = 6, HelpMessage = "The Display Name")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets Project.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The project object")]
        public object Project { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// The process record.
        /// </summary>
        /// <exception cref="PSInvalidOperationException">
        /// An error occured
        /// </exception>
        protected override void ProcessRecord()
        {
            var project = this.Project as VSProject;

            if (project == null)
            {
                throw new PSInvalidOperationException("Cannot cast the Project object to a VSProject");
            }

            var sp = new ServiceProvider((IOleServiceProvider)project.DTE);

            // Get the toolbox
            var svsToolbox = sp.GetService(typeof(SVsToolbox));

            if (svsToolbox == null)
            {
                throw new PSInvalidOperationException("Cannot get global Toolbox Service (SVsToolbox)");
            }

            var toolbox = svsToolbox as IVsToolbox;

            if (toolbox == null)
            {
                throw new PSInvalidOperationException("Cannot cast Toolbox Service to IVsToolbox");
            }

            // Add the tab
            toolbox.AddTab(this.Category);

            // Find the assembly in the project references
            var reference = project.References.Find(this.ActivityAssembly);

            if (reference == null)
            {
                throw new PSInvalidOperationException(
                    "Cannot find a project reference to assembly " + this.ActivityAssembly);
            }

            // Load the assembly
            // Don't load the assembly - this causes problems when uninstalling - see http://wf.codeplex.com/workitem/8762
            // var assembly = Assembly.LoadFrom(reference.Path);

            // Get the activity type
            // var activityType = assembly.GetType(this.Activity);
            var assemblyQualifiedName = GetAssemblyQualifiedName(reference.Path, this.Activity);

            if (string.IsNullOrEmpty(this.DisplayName))
            {
                this.DisplayName = GetNameFromActivity(this.Activity);
            }

            IEnumToolboxItems enumToolboxItems;
            toolbox.EnumItems(this.Category, out enumToolboxItems);
            var dataObjects = new IDataObject[1];
            uint fetched;

            while (enumToolboxItems.Next(1, dataObjects, out fetched) == VSConstants.S_OK)
            {
                if (dataObjects[0] != null && fetched == 1)
                {
                    // Create an OleDataObject to work with
                    var itemDataObject = new OleDataObject(dataObjects[0]);

                    // Access the data
                    var name = itemDataObject.GetData("CF_WORKFLOW_4");

                    if (name != null)
                    {
                        // If this toolbox item already exists, remove it
                        if (name.ToString() == this.DisplayName)
                        {
                            // Note: This prevents an old toolbox item from adding a ref to the wrong runtime assembly
                            toolbox.RemoveItem(dataObjects[0]);
                        }
                    }
                }
            }

            var dataObject = new OleDataObject();
            dataObject.SetData("AssemblyName", this.ActivityAssembly);
            dataObject.SetData("CF_WORKFLOW_4", this.DisplayName);
            dataObject.SetData("WorkflowItemTypeNameFormat", assemblyQualifiedName);

            // Load the bitmap
            var bitmap = (Bitmap)Resources.ResourceManager.GetObject(this.BitmapID);

            if (bitmap == null)
            {
                throw new PSInvalidOperationException("Cannot load bitmap ID " + this.BitmapID);
            }

            var toolboxItemInfo = new TBXITEMINFO[1];
            toolboxItemInfo[0].bstrText = this.DisplayName;
            toolboxItemInfo[0].hBmp = bitmap.GetHbitmap();
            toolboxItemInfo[0].clrTransparent = (uint)ColorTranslator.ToWin32(Color.White);

            toolbox.AddItem(dataObject, toolboxItemInfo, this.Category);
        }

        /// <summary>
        /// The get assembly qualified name.
        /// </summary>
        /// <param name="assemblyPath">
        /// The assembly path.
        /// </param>
        /// <param name="activityName">
        /// The activity name.
        /// </param>
        /// <returns>
        /// The assembly qualified name.
        /// </returns>
        private static string GetAssemblyQualifiedName(string assemblyPath, string activityName)
        {
            var domain = AppDomain.CreateDomain(
                "AssemblyLoad", 
                null, 
                new AppDomainSetup { ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) });

            domain.Load(Assembly.GetExecutingAssembly().GetName().FullName);
            var worker =
                (AssemblyLoadWorker)
                domain.CreateInstanceAndUnwrap(
                    Assembly.GetExecutingAssembly().GetName().FullName, 
                    "Microsoft.Activities.Extensions.NuGet.AssemblyLoadWorker");

            var assemblyQualifiedName = worker.GetFullName(assemblyPath, activityName);

            AppDomain.Unload(domain);

            return assemblyQualifiedName;
        }

        /// <summary>
        /// The get name from activity.
        /// </summary>
        /// <param name="fullName">
        /// The full name.
        /// </param>
        /// <returns>
        /// The name from the activity.
        /// </returns>
        private static string GetNameFromActivity(string fullName)
        {
            // Script passes full name like this
            // "Microsoft.Activities.Extensions.Statements.DelayUntilTime"
            var parts = fullName.Split('.');
            return parts.Last();
        }

        #endregion
    }
}