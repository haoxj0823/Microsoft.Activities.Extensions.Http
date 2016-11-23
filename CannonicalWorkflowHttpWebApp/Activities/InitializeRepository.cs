// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializeRepository.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CannonicalWorkflowHttpWebApp.Activities
{
    using System.Activities;
    using System.Net;
    using System.Net.Http;

    using CannonicalWorkflowHttpWebApp.Infrastructure;
    using CannonicalWorkflowHttpWebApp.Models;

    /// <summary>
    /// The initialize repository.
    /// </summary>
    public sealed class InitializeRepository : CodeActivity<object>
    {
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
        }

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The HttpResponse message
        /// </returns>
        protected override object Execute(CodeActivityContext context)
        {
            SampleResourceRepository.Initialize();
            return new HttpResponseMessage<Sample>(HttpStatusCode.NoContent);
        }

        #endregion
    }
}