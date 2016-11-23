// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CheckIfMatch.cs" company="Microsoft">
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
    /// The check if match.
    /// </summary>
    public sealed class CheckIfMatch : CodeActivity
    {
        #region Public Properties

        /// <summary>
        ///   Gets or sets ETag.
        /// </summary>
        public InArgument<string> ETag { get; set; }

        /// <summary>
        ///   Gets or sets Request.
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
            if (this.Request.Get(context).Headers.IfMatch.Any(etag => EntityTag.IsMatchingTag(this.ETag.Get(context), etag.Tag)))
            {
                throw new HttpResponseException(HttpStatusCode.PreconditionFailed);
            }
        }



        #endregion
    }
}