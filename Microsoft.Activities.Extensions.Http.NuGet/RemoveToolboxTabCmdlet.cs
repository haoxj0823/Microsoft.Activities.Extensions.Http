// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RemoveToolboxTabCmdlet.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Extensions.Http.NuGet
{
    using System.Management.Automation;

    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using VSLangProj;

    using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    /// <summary>
    /// The remove toolbox tab cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "ToolboxTab")]
    public class RemoveToolboxTabCmdlet : PSCmdlet
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets Category.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The toolbox category (tab name)")]
        public string Category { get; set; }

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

            toolbox.RemoveTab(this.Category);
        }

        #endregion
    }
}