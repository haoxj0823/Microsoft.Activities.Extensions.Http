// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeleteFromRepository.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CannonicalWorkflowHttpWebApp.Activities
{
    using System;
    using System.Activities;
    using System.Linq;
    using System.Net;
    using System.Net.Http;

    using CannonicalWorkflowHttpWebApp.Infrastructure;
    using CannonicalWorkflowHttpWebApp.Models;

    using Microsoft.Activities;
    using Microsoft.Activities.Extensions;
    using Microsoft.Activities.Http;
    using Microsoft.ApplicationServer.Http.Dispatcher;

    /// <summary>
    /// The delete from repository.
    /// </summary>
    public sealed class DeleteFromRepository : CodeActivity<object>
    {
        #region Public Properties

        /// <summary>
        ///   Gets or sets Key.
        /// </summary>
        public InArgument<string> Key { get; set; }

        /// <summary>
        ///   Gets or sets Request.
        /// </summary>
        public InArgument<HttpRequestMessage> Request { get; set; }

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
            metadata.AddAndBindArgument(this.Request, new RuntimeArgument("Request", typeof(HttpRequestMessage), ArgumentDirection.In));
        }

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The response message
        /// </returns>
        protected override object Execute(CodeActivityContext context)
        {
            if (this.Request == null)
            {
                throw new InvalidOperationException("Request is null");
            }

            var repository = context.GetExtension<IResourceRepository<int, Sample>>();

            var resource = repository.Delete(
                int.Parse(this.Key.Get(context)), r => CheckIfMatch(this.Request.Get(context), r));

            // If no resource was not found (because it was previously deleted), return No Content
            return resource == null
                       ? new HttpResponseMessage(HttpStatusCode.NoContent)
                       : new HttpResponseMessage<Sample>(resource);
        }

        /// <summary>
        /// The check conditional update.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <param name="resource">
        /// The resource.
        /// </param>
        /// <exception cref="HttpResponseException">
        /// The precondition failed
        /// </exception>
        private static void CheckIfMatch(HttpRequestMessage request, Sample resource)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (resource == null)
            {
                throw new ArgumentNullException("resource");
            }

            // No etags
            if (request.Headers.IfMatch.Count == 0)
            {
                return;
            }

            // If there is no matching etag, the pre-condition fails
            if (!request.Headers.IfMatch.Any(etag => EntityTag.IsMatchingTag(resource.Tag, etag.Tag)))
            {
                throw new HttpResponseException(HttpStatusCode.PreconditionFailed);
            }
        }

        #endregion
    }
}