// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityTag.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http
{
    using System;
    using System.Linq;
    using System.Net.Http;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class EntityTag
    {
        #region Public Methods

        /// <summary>
        /// The if match.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <param name="etag">
        /// The etag.
        /// </param>
        /// <returns>
        /// true if the request contains a header that matches the tag
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The request was null
        /// </exception>
        public static bool IfMatch(HttpRequestMessage request, string etag)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return request.Headers.IfMatch.Any(entityTagHeader => IsMatchingTag(etag, entityTagHeader.Tag));
        }

        public static bool IfNoneMatch(HttpRequestMessage request, string etag)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return !request.Headers.IfNoneMatch.Any(entityTagHeader => IsMatchingTag(etag, entityTagHeader.Tag));
        }

        /// <summary>
        /// The is matching tag.
        /// </summary>
        /// <param name="resourceTag">
        /// The resource tag.
        /// </param>
        /// <param name="etag">
        /// The etag.
        /// </param>
        /// <returns>
        /// true if there is a matching tag, false if not
        /// </returns>
        public static bool IsMatchingTag(string resourceTag, string etag)
        {
            // "*" wildcard matches any value
            return etag == "\"*\"" || etag == QuotedString.Get(resourceTag);
        }

        #endregion
    }
}