// -----------------------------------------------------------------------
// <copyright file="WorkflowCookieCorrelation.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Activities.Http.Activation
{
    using System;
    using System.Linq;
    using System.Net.Http;

    /// <summary>
    ///   Provides cookie support for a workflow instance
    /// </summary>
    public class WorkflowCookieCorrelation : IHttpWorkflowCorrelation
    {
        #region Public Methods

        public static void AddCookie(
            HttpResponseMessage response, Guid instanceId)
        {
            response.Headers.Add(
                HttpNames.SetCookie,
                string.Format("{0}={1}", HttpNames.WorkflowInstance, instanceId));
        }

        public static void AddCookie(
            HttpRequestMessage request, Guid instanceId)
        {
            request.Headers.Add(
                HttpNames.Cookie,
                string.Format("{0}={1}", HttpNames.WorkflowInstance, instanceId));
        }

        public static Guid FromRequest(HttpRequestMessage request)
        {
            var cookieHeaders = from header in request.Headers
                                where header.Key == HttpNames.Cookie
                                select header.Value;

            foreach (
                var guid in
                    cookieHeaders.SelectMany(
                        cookieValues =>
                        cookieValues.Where(
                            value =>
                            value.StartsWith(HttpNames.WorkflowInstance))).
                        Select(GetInstanceId).Where(guid => guid != Guid.Empty))
            {
                return guid;
            }

            return Guid.Empty;
        }

        public static Guid FromResponse(HttpResponseMessage response)
        {
            var cookieHeaders = from header in response.Headers
                                where header.Key == HttpNames.SetCookie
                                select header.Value;

            foreach (
                var guid in
                    cookieHeaders.SelectMany(
                        cookieValues =>
                        cookieValues.Where(
                            value =>
                            value.StartsWith(HttpNames.WorkflowInstance))).
                        Select(GetInstanceId).Where(guid => guid != Guid.Empty))
            {
                return guid;
            }

            return Guid.Empty;
        }

        #endregion

        #region Implemented Interfaces

        #region IHttpWorkflowCorrelation

        public Guid CorrelateRequest(HttpRequestMessage request)
        {
            return FromRequest(request);
        }

        #endregion

        #endregion

        #region Methods

        private static Guid GetInstanceId(string cookieValue)
        {
            var parts = cookieValue.Split('=');
            if (parts.Length == 2)
            {
                Guid guid;
                Guid.TryParse(parts[1], out guid);
                return guid;
            }
            return Guid.Empty;
        }

        #endregion
    }
}