// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PutInRepository.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CannonicalWorkflowHttpWebApp.Activities
{
    using System.Activities;
    using System.Diagnostics;

    using CannonicalWorkflowHttpWebApp.Infrastructure;
    using CannonicalWorkflowHttpWebApp.Models;

    using Microsoft.Activities;
    using Microsoft.Activities.Extensions;

    /// <summary>
    /// The put in repository.
    /// </summary>
    public sealed class PutInRepository : CodeActivity
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets ExistingSample.
        /// </summary>
        public InArgument<Sample> ExistingSample { get; set; }

        /// <summary>
        /// Gets or sets Key.
        /// </summary>
        public InArgument<string> Key { get; set; }

        /// <summary>
        /// Gets or sets Sample.
        /// </summary>
        public InOutArgument<Sample> Sample { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates and validates a description of the activity’s arguments, variables, child activities, and activity delegates.
        /// </summary>
        /// <param name="metadata">
        /// The activity’s metadata that encapsulates the activity’s arguments, variables, child activities, and activity delegates.
        /// </param>
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            metadata.RequireExtension<IResourceRepository<int, Sample>>();
            metadata.AddDefaultExtensionProvider(() => SampleResourceRepository.Current);
            metadata.AddAndBindArgument(this.Key, new RuntimeArgument("Key", typeof(string), ArgumentDirection.In));
            metadata.AddAndBindArgument(
                this.Sample, new RuntimeArgument("Sample", typeof(Sample), ArgumentDirection.InOut));
            metadata.AddAndBindArgument(
                this.ExistingSample, new RuntimeArgument("ExistingSample", typeof(Sample), ArgumentDirection.In));
        }

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        protected override void Execute(CodeActivityContext context)
        {
            var repository = context.GetExtension<IResourceRepository<int, Sample>>();
            var result = repository.Put(
                int.Parse(this.Key.Get(context)), this.Sample.Get(context), this.ExistingSample.Get(context));
            this.Sample.Set(context, result);
            Trace.WriteLine(string.Format("Existing tag {0}, New tag {1}", this.ExistingSample.Get(context).Tag, result.Tag));
        }

        #endregion
    }
}