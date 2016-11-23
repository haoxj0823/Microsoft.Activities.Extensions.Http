// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpWorkflowService.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Activities
{
    using System.Activities;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Markup;

    /// <summary>
    /// A Workflow Service that uses HTTP messaging
    /// </summary>
    [ContentProperty("Body")]
    [ToolboxBitmap(typeof(HttpReceive), "HttpReceive16.png")]
    public class HttpWorkflowService : NativeActivity
    {
        #region Constants and Fields

        /// <summary>
        /// The variables.
        /// </summary>
        private Collection<Variable> variables;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "HttpWorkflowService" /> class.
        /// </summary>
        public HttpWorkflowService()
        {
            this.Receives = new Collection<Activity>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets Receives.
        /// </summary>
        [DefaultValue(null)]
        public Collection<Activity> Receives { get; set; }

        /// <summary>
        /// Gets Variables.
        /// </summary>
        public Collection<Variable> Variables
        {
            get
            {
                return this.variables ?? (this.variables = new Collection<Variable>());
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The cache metadata.
        /// </summary>
        /// <param name="metadata">
        /// The metadata.
        /// </param>
        /// <exception cref="ValidationException">
        /// The workflow is invalid
        /// </exception>
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            if (
                this.Receives.Any(
                    r => !r.GetType().IsAssignableFrom(typeof(HttpReceive))))
            {
                throw new ValidationException(
                    "HttpWorkflowService can contain only HttpReceive activities");
            }

            metadata.SetChildrenCollection(this.Receives);
            metadata.SetVariablesCollection(this.Variables);
        }

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        protected override void Execute(NativeActivityContext context)
        {
            // Schedule all the receives
            foreach (var activity in this.Receives)
            {
                context.ScheduleActivity(activity, this.OnReceiveCompleted);
            }
        }

        /// <summary>
        /// The on receive completed.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="completedInstance">
        /// The completed instance.
        /// </param>
        private void OnReceiveCompleted(
            NativeActivityContext context, ActivityInstance completedInstance)
        {
            // Cancel the remaining children
            context.CancelChildren();
        }

        #endregion
    }
}