// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpReceive.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Activities
{
    using System;
    using System.Activities;
    using System.Activities.Statements;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web.Query.Dynamic;

    using Microsoft.Activities.Http.Activation;

    /// <summary>
    /// The http receive.
    /// </summary>
    [ToolboxBitmap(typeof(HttpReceive), "HttpReceive16.png")]
    public sealed class HttpReceive : NativeActivity
    {
        #region Constants and Fields

        /// <summary>
        /// The json media type.
        /// </summary>
        private static readonly MediaTypeWithQualityHeaderValue JsonMediaType =
            new MediaTypeWithQualityHeaderValue("application/json");

        /// <summary>
        /// The text json media type.
        /// </summary>
        private static readonly MediaTypeWithQualityHeaderValue TextJsonMediaType =
            new MediaTypeWithQualityHeaderValue("text/json");

        /// <summary>
        /// The text xml media type.
        /// </summary>
        private static readonly MediaTypeWithQualityHeaderValue TextXmlMediaType =
            new MediaTypeWithQualityHeaderValue("text/xml");

        /// <summary>
        /// The xml media type.
        /// </summary>
        private static readonly MediaTypeWithQualityHeaderValue XmlMediaType =
            new MediaTypeWithQualityHeaderValue("application/xml");

        /// <summary>
        /// The no persist handle.
        /// </summary>
        private readonly Variable<NoPersistHandle> noPersistHandle = new Variable<NoPersistHandle>();

        /// <summary>
        /// The persist.
        /// </summary>
        private readonly Activity persist = new Persist();

        /// <summary>
        /// The host context.
        /// </summary>
        private IHttpWorkflowHostContext hostContext;

        /// <summary>
        /// The receive context.
        /// </summary>
        private IHttpWorkflowReceiveContext receiveContext;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpReceive"/> class.
        /// </summary>
        public HttpReceive()
        {
            this.Body = new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets Body.
        /// </summary>
        [DefaultValue(null)]
        public ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object> Body { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether the operation causes a new workflow service instance to be created.
        /// </summary>
        public bool CanCreateInstance { get; set; }

        /// <summary>
        ///   Gets or sets the HTTP Method to listen for
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether the workflow service instance should be persisted before sending the message.
        /// </summary>
        public bool PersistBeforeSend { get; set; }

        /// <summary>
        ///   Gets or sets the Uri Template to be used for matching requests
        /// </summary>
        /// <remarks>
        ///   You can includes named arguments in the UriTemplate
        /// </remarks>
        public string UriTemplate { get; set; }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether CanInduceIdle.
        /// </summary>
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The get bookmark name.
        /// </summary>
        /// <param name="baseAddress">
        /// The base address.
        /// </param>
        /// <returns>
        /// The bookmark name
        /// </returns>
        internal string GetBookmarkName(Uri baseAddress)
        {
            return string.Format("{0}|{1}", this.Method, new Uri(baseAddress, this.UriTemplate));
        }

        /// <summary>
        /// The cache metadata.
        /// </summary>
        /// <param name="metadata">
        /// The metadata.
        /// </param>
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.RequireExtension<IHttpWorkflowHostContext>();
            metadata.AddDelegate(this.Body);
            metadata.AddImplementationVariable(this.noPersistHandle);
            metadata.AddImplementationChild(this.persist);
        }

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        protected override void Execute(NativeActivityContext context)
        {
            this.hostContext = context.GetExtension<IHttpWorkflowHostContext>();

            // Typically there is only one base address but there could be more than one.
            foreach (var bookmarkName in this.hostContext.BaseAddresses.Select(this.GetBookmarkName))
            {
                context.CreateBookmark(bookmarkName, this.ReceiveCallback);
                context.Track(new HttpReceiveMessageRecord(bookmarkName));
            }
        }

        /// <summary>
        /// The get negotiated media type.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <returns>
        /// A media type header value
        /// </returns>
        private static MediaTypeWithQualityHeaderValue GetNegotiatedMediaType(HttpRequestMessage request)
        {
            if (request.Headers.Accept.Contains(JsonMediaType) || request.Headers.Accept.Contains(TextJsonMediaType))
            {
                return JsonMediaType;
            }

            return XmlMediaType;
        }

        /// <summary>
        /// The on body completed.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="completedinstance">
        /// The completed instance.
        /// </param>
        /// <param name="result">
        /// The result.
        /// </param>
        private void OnBodyCompleted(NativeActivityContext context, ActivityInstance completedinstance, object result)
        {
            this.noPersistHandle.Get(context).Exit(context);

            // The body activities can create a response message or response may be data

            // Create the response message
            var response = result as HttpResponseMessage ?? new HttpResponseMessage();

            // If the result was data, add it to the content
            if (!(result is HttpResponseMessage) && result != null)
            {
                object content;
                Type contentType;
                if (result is IQueryable)
                {
                    var listType = typeof(List<>).MakeGenericType(((IQueryable)result).ElementType);
                    content = (IList)Activator.CreateInstance(listType, result);
                    contentType = listType;
                }
                else
                {
                    content = result;
                    contentType = result.GetType();
                }

                response.Content = new ObjectContent(
                    contentType, content, GetNegotiatedMediaType(this.receiveContext.Request));
            }

            WorkflowCookieCorrelation.AddCookie(response, context.WorkflowInstanceId);
            this.receiveContext.Response = response;

            context.Track(new HttpReceiveResponseRecord(result));

            if (this.PersistBeforeSend)
            {
                context.ScheduleActivity(this.persist);
            }
        }

        /// <summary>
        /// The receive callback.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="bookmark">
        /// The bookmark.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        private void ReceiveCallback(NativeActivityContext context, Bookmark bookmark, object value)
        {
            context.RemoveAllBookmarks();
            this.receiveContext = (IHttpWorkflowReceiveContext)value;
            this.hostContext = context.GetExtension<IHttpWorkflowHostContext>();

            // bind the parameters using the UriTemplate
            var match = this.hostContext.MatchSingle(this.receiveContext.Request);

            this.noPersistHandle.Get(context).Enter(context);

            // TODO: Consider fault handling - do we need to do anything special?
            context.ScheduleFunc(
                this.Body, 
                this.receiveContext.Request, 
                match.BoundVariables.AllKeys.ToDictionary(s => s, key => match.BoundVariables[key]), 
                this.OnBodyCompleted);
        }

        #endregion
    }
}