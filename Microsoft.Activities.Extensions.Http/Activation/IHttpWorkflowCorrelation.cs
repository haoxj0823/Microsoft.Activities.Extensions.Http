// -----------------------------------------------------------------------
// <copyright file="IHttpWorkflowCorrelation.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Activities.Http.Activation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;

    /// <summary>
    /// Provides correlation services on an incoming message
    /// </summary>
    public interface IHttpWorkflowCorrelation
    {
        Guid CorrelateRequest(HttpRequestMessage request);
    }
}
