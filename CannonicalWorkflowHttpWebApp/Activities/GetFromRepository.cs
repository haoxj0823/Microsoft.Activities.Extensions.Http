// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetFromRepository.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CannonicalWorkflowHttpWebApp.Activities
{
    using System;
    using System.Activities;
    using Microsoft.Activities;
    using Microsoft.Activities.Extensions;

    using CannonicalWorkflowHttpWebApp.Infrastructure;
    using CannonicalWorkflowHttpWebApp.Models;

    /// <summary>
    /// Gets a sample resource from the repository
    /// </summary>
    public sealed class GetFromRepository : CodeActivity<Sample>
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets Key.
        /// </summary>
        public InArgument<string> Key { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates and validates a description of the activity’s arguments, variables, child activities, and activity delegates.
        /// </summary>
        /// <param name="metadata">The activity’s metadata that encapsulates the activity’s arguments, variables, child activities, and activity delegates.</param>
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            metadata.RequireExtension<IResourceRepository<int, Sample>>();
            metadata.AddDefaultExtensionProvider(() => SampleResourceRepository.Current);
            metadata.AddAndBindArgument(this.Key, new RuntimeArgument("Key", typeof(string), ArgumentDirection.In));
        }

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The sample resource
        /// </returns>
        protected override Sample Execute(CodeActivityContext context)
        {
            var repository = context.GetExtension<IResourceRepository<int, Sample>>();

            return repository.Get(int.Parse(this.Key.Get(context)));
        }

        #endregion
    }
}