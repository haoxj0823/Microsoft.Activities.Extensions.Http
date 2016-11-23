namespace Microsoft.Activities.Http.Activation
{
    using System;
    using System.Collections.ObjectModel;
    using System.Net.Http;

    internal interface IHttpWorkflowHostContext
    {
        ReadOnlyCollection<Uri> BaseAddresses { get; }

        /// <summary>
        ///   Searches the uriTemplate tables collection for a match
        /// </summary>
        /// <param name = "request">The Request URI</param>
        /// <returns>A UriTemplateMatch or null if not found</returns>
        UriTemplateMatch MatchSingle(HttpRequestMessage request);
    }
}