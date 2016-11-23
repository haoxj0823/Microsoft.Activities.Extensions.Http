// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHttpWorkflowReceiveContext.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Activation
{
    using System.Net.Http;

    /// <summary>
    /// The i http workflow receive context.
    /// </summary>
    public interface IHttpWorkflowReceiveContext
    {
        #region Public Properties

        /// <summary>
        /// Gets Request.
        /// </summary>
        HttpRequestMessage Request { get; }

        /// <summary>
        /// Gets or sets Response.
        /// </summary>
        HttpResponseMessage Response { get; set; }

        #endregion
    }
}