// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpWorkflowServiceDesigner.xaml.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Design
{
    using System.Activities.Presentation.Metadata;
    using System.ComponentModel;

    using Microsoft.Activities.Http.Activities;

    /// <summary>
    /// The http workflow service designer.
    /// </summary>
    public partial class HttpWorkflowServiceDesigner
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpWorkflowServiceDesigner"/> class.
        /// </summary>
        public HttpWorkflowServiceDesigner()
        {
            this.InitializeComponent();            
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The register metadata.
        /// </summary>
        /// <param name="builder">
        /// The builder.
        /// </param>
        public static void RegisterMetadata(AttributeTableBuilder builder)
        {
            builder.AddCustomAttributes(
                typeof(HttpWorkflowService), new DesignerAttribute(typeof(HttpWorkflowServiceDesigner)));
            builder.AddCustomAttributes(
                typeof(HttpWorkflowService), new DescriptionAttribute("A Workflow Service that uses HTTP Messaging"));
        }

        #endregion
    }
}