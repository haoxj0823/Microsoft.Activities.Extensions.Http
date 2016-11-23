// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DynamicQueryHandler.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Activities
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Reflection;

    using Microsoft.ApplicationServer.Http.Dispatcher;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DynamicQueryHandler :
        HttpOperationHandler<HttpRequestMessage, HttpResponseMessage, HttpResponseMessage>
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "DynamicQueryHandler" /> class.
        /// </summary>
        public DynamicQueryHandler()
            : base("emptyDummy")
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Processes the response to see if it contains an 
        ///   <see cref="IQueryable"/> and if so invokes the QueryCompositionHandler and QueryDeserialization Handler.
        /// </summary>
        /// <param name="request">
        /// The <see cref="HttpRequestMessage"/> associated with the current request.
        /// </param>
        /// <param name="response">
        /// The response.
        /// </param>
        /// <returns>
        /// Return value is ignored.
        /// </returns>
        protected override HttpResponseMessage OnHandle(HttpRequestMessage request, HttpResponseMessage response)
        {
            if (response != null)
            {
                IQueryable source = ((IEnumerable)((ObjectContent)response.Content).ReadAs()).AsQueryable();
                var queryableType = typeof(IQueryable<>).MakeGenericType(source.ElementType);
                var qdh = new QueryDeserializationHandler(queryableType);
                qdh.Handle(new object[] { request });

                var qch = new QueryCompositionHandler(queryableType);
                return qch.Handle(new object[] { request, response })[0] as HttpResponseMessage;
            }
            else
            {
                return response;
            }
        }

        #endregion
    }
}