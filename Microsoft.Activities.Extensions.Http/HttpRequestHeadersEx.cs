// -----------------------------------------------------------------------
// <copyright file="HttpRequestHeadersEx.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Activities.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class HttpRequestHeadersEx
    {
        public static string GetHeaderValue(this HttpRequestHeaders headers, string name)
        {
            IEnumerable<string> values;
            if (headers.TryGetValues(name, out values))
            {
                return values.FirstOrDefault();
            }

            return null;
        }
    }
}
