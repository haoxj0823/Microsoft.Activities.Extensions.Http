// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CheckIfNoneMatch.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Activities
{
    using System.Activities;
    using System.Linq;
    using System.Net;
    using System.Net.Http;

    using Microsoft.ApplicationServer.Http.Dispatcher;

    /// <summary>
    /// The CheckIfNoneMatch activity looks at the request message for an IfNoneMatch header and checks against the supplied ETag value
    /// </summary>
    public sealed class CheckIfNoneMatch : CodeActivity
    {
        // Define an activity input argument of type string
        #region Public Properties

        /// <summary>
        /// Gets or sets ETag.
        /// </summary>
        public InArgument<string> ETag { get; set; }

        /// <summary>
        /// Gets or sets Request.
        /// </summary>
        public InArgument<HttpRequestMessage> Request { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <exception cref="HttpResponseException">
        /// There is a matching ETag
        /// </exception>
        protected override void Execute(CodeActivityContext context)
        {
            if (
                this.Request.Get(context).Headers.IfNoneMatch.Any(
                    etag => EntityTag.IsMatchingTag(this.ETag.Get(context), etag.Tag)))
            {
                throw new HttpResponseException(HttpStatusCode.NotModified);
            }
        }


        #endregion
    }
}