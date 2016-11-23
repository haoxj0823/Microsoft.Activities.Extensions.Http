// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetAllResources.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CannonicalWorkflowHttpWebApp.Activities
{
    using System.Activities;
    using System.Linq;

    using CannonicalWorkflowHttpWebApp.Infrastructure;
    using CannonicalWorkflowHttpWebApp.Models;

    /// <summary>
    /// The get all resources.
    /// </summary>
    public sealed class GetAllResources : CodeActivity<object>
    {
        #region Methods

        /// <summary>
        /// Creates and validates a description of the activity’s arguments, variables, child activities, and activity delegates.
        /// </summary>
        /// <param name="metadata">The activity’s metadata that encapsulates the activity’s arguments, variables, child activities, and activity delegates.</param>
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            metadata.RequireExtension<IResourceRepository<int, Sample>>();
            metadata.AddDefaultExtensionProvider(() => SampleResourceRepository.Current);
        }

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// An IQueryable(Of Sample)
        /// </returns>
        protected override object Execute(CodeActivityContext context)
        {
            var repository = context.GetExtension<IResourceRepository<int, Sample>>();

            return repository.Resources.AsQueryable();
        }

        #endregion
    }
}