// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateHttpResponse.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Activities
{
    using System.Activities;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    /// <summary>
    /// The create http response.
    /// </summary>
    /// <typeparam name="T">
    /// The type of content you want to return
    /// </typeparam>
    public sealed class CreateHttpResponse<T> : CodeActivity<object>
    {
        public CreateHttpResponse()
        {
            StatusCode = HttpStatusCode.OK;
        }

        #region Public Properties

        /// <summary>
        /// Gets or sets Content.
        /// </summary>
        public InArgument<T> Content { get; set; }

        /// <summary>
        /// Gets or sets ETag.
        /// </summary>
        public InArgument<string> ETag { get; set; }


        public HttpStatusCode StatusCode { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The HTTP Response message
        /// </returns>
        protected override object Execute(CodeActivityContext context)
        {
            if (this.Content.Expression == null)
            {
                return new HttpResponseMessage<T>(this.StatusCode);
            }
            else
            {
                var response = new HttpResponseMessage<T>(this.Content.Get(context), this.StatusCode);

                if (!(this.ETag == null || this.ETag.Get(context) == null))
                {
                    response.Headers.ETag = new EntityTagHeaderValue(QuotedString.Get(this.ETag.Get(context)));
                }

                return response;
            }
        }

        #endregion
    }
}