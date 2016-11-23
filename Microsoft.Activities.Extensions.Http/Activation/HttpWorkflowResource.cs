// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpWorkflowResource.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Activation
{
    using System;
    using System.Activities;
    using System.Activities.Tracking;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Web;
    using System.Threading;
    using Microsoft.Activities.Extensions;
    using Microsoft.Activities.Extensions.Diagnostics;
    using Microsoft.Activities.Http.Activities;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.Activities.UnitTesting.Activities;

    /// <summary>
    /// The http workflow resource.
    /// </summary>
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class HttpWorkflowResource : IHttpWorkflowReceiveContext
    {
        #region Constants and Fields

        /// <summary>
        ///   The service host.
        /// </summary>
        private readonly HttpWorkflowServiceHost serviceHost;

#if DEBUG

        /// <summary>
        ///   The test name.
        /// </summary>
        private string testName;

        /// <summary>
        ///   The test info.
        /// </summary>
        private string testInfo;
#endif

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpWorkflowResource"/> class.
        /// </summary>
        /// <param name="serviceHost">
        /// The service host.
        /// </param>
        /// <param name="requestMessage">
        /// The request message.
        /// </param>
        public HttpWorkflowResource(HttpWorkflowServiceHost serviceHost, HttpRequestMessage requestMessage)
        {
            this.serviceHost = serviceHost;
            this.Request = requestMessage;
        }

        #endregion

        #region Properties

        /// <summary>
        ///   Gets BaseAddresses.
        /// </summary>
        public IEnumerable<Uri> BaseAddresses
        {
            get
            {
                return this.serviceHost.BaseAddresses;
            }
        }

        /// <summary>
        ///   Gets ServiceHost.
        /// </summary>
        public HttpWorkflowServiceHost ServiceHost
        {
            get
            {
                return this.serviceHost;
            }
        }

#if DEBUG

        /// <summary>
        /// The to string.
        /// </summary>
        /// <returns>
        /// A string with info
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "HttpWorkflowResource Hash:{0} TestName: {1} TestInfo: {2}",
                this.GetHashCode(),
                this.testName,
                this.testInfo);
        }

#endif

        /// <summary>
        ///   Gets Request.
        /// </summary>
        public HttpRequestMessage Request { get; private set; }

        /// <summary>
        ///   The response.
        /// </summary>
        private HttpResponseMessage response;

        /// <summary>
        ///   Gets or sets Response.
        /// </summary>
        public HttpResponseMessage Response
        {
            get
            {
                return this.response;
            }

            set
            {
                this.response = value;
                this.responseEvent.Set();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Invoked when a message is received at this base address for any Uri or Method
        /// </summary>
        /// <remarks>
        /// When a message is received there are several possibilities
        ///   The message may not find a matching Uri template - if so it will throw an exception
        ///   If a receive activity with the matching Uri template is found there are several possibilities
        ///   * If no correlation and CanCreateInstance true - create a new instance
        ///   * If correlation, load instance and resume bookmark
        ///   Instances are cached
        /// </remarks>
        /// <returns>
        /// A response message
        /// </returns>
        [WebInvoke(UriTemplate = "*", Method = "*")]
        public HttpResponseMessage InvokeWorkflow()
        {
#if DEBUG
            this.testName = this.Request.Headers.GetHeaderValue("TestName");
            this.testInfo = this.Request.Headers.GetHeaderValue("TestInfo");
            HttpExceptionHelper.WriteThread(
                "HttpResponseMessage InvokeWorkflow() message received {0} {1} Test: \"{2}\" Info:{3}",
                this.Request.Method,
                this.Request.RequestUri,
                this.testName,
                this.testInfo);
#endif

            // Step 1 - locate a receive activity that matches the Uri template
            // This will throw if no matching activity is found
            var receive = this.SelectMatchingReceiveActivity();

            if (receive == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            // Step 2 - Load or Create a workflow instance
            this.LoadOrCreateWorkflowInstance();

            HttpExceptionHelper.WriteThread(
                "Trying to Lock instance {0} {1} {2} Test: \"{3}\" Info:{4}",
                this.workflowApplication.Id,
                this.Request.Method,
                this.Request.RequestUri,
                this.testName,
                this.testInfo);
            if (!Monitor.TryEnter(this.workflowApplication, this.ServiceHost.WorkflowTimeout))
            {
                throw HttpExceptionHelper.CreateResponseException(
                    HttpStatusCode.InternalServerError,
                    "Timeout waiting for lock on instance {0}",
                    this.workflowApplication.Id);
            }

            try
            {
                HttpExceptionHelper.WriteThread(
                    "Successfully Locked instance {0} {1} {2} Test: \"{3}\" Info:{4}",
                    this.workflowApplication.Id,
                    this.Request.Method,
                    this.Request.RequestUri,
                    this.testName,
                    this.testInfo);

                // There may be a bookmark pending for this receive activity
                var bookmarkName =
                    receive.GetBookmarkName(this.serviceHost.GetMatchingBaseAddress(this.Request.RequestUri));

                // If the instance does not have the bookmark, create and run it
                if (!this.workflowApplication.ContainsBookmark(bookmarkName))
                {
                    // This will run the workflow until it becomes idle with the named bookmark
                    // Or it aborts, times out or completes
                    this.RunUntilBookmark(bookmarkName);
                }

                // Deliver the message to the receive activity by resuming the bookmark
                this.DeliverMessage(bookmarkName, receive);

                // The receive activity may or may not set a response
                HttpExceptionHelper.WriteThread("Returning response {0}", this.Response);

                // TODO: This makes IQueryable not work - can we return a response as an object?
                // If we do return an object, what if it is already an HttpResponse?
                return this.Response;
            }
            finally
            {
                HttpExceptionHelper.WriteThread(
                    "Unlocking instance {0} {1} {2} Test: \"{3}\" Info:{4}",
                    this.workflowApplication.Id,
                    this.Request.Method,
                    this.Request.RequestUri,
                    this.testName,
                    this.testInfo);
                Monitor.Exit(this.workflowApplication);
            }
        }

        /// <summary>
        /// Delivers the Http message to the receive activity and waits until the receive is closed before returning
        /// </summary>
        /// <param name="bookmarkName">
        /// The name of the bookmark to wait for
        /// </param>
        /// <param name="receive">
        /// The HttpReceive activity to deliver to
        /// </param>
        private void DeliverMessage(string bookmarkName, HttpReceive receive)
        {
            HttpExceptionHelper.WriteThread(
                "Delivering message to HttpReceive activity {0} ID {1} \"{2}\"",
                bookmarkName,
                receive.Id,
                receive.DisplayName);
            var timeLeft = this.serviceHost.WorkflowTimeout;
            var retryMsecs = 10;
            while (timeLeft > TimeSpan.Zero)
            {
                // When resuming, the workflow might not be ready
                var result = this.workflowApplication.ResumeBookmark(bookmarkName, this);
                switch (result)
                {
                    case BookmarkResumptionResult.Success:
                        if (!this.responseEvent.WaitOne(timeLeft))
                        {
                            throw HttpExceptionHelper.CreateResponseException(
                                HttpStatusCode.InternalServerError,
                                "Workflow timeout while waiting for response for {0}",
                                bookmarkName);
                        }

                        // If the workflow terminated with an exception throw it
                        if (this.terminationException != null)
                        {
                            throw this.terminationException;
                        }

                        return;

                    case BookmarkResumptionResult.NotFound:
                        throw HttpExceptionHelper.CreateResponseException(
                            HttpStatusCode.InternalServerError,
                            "Workflow was not waiting for bookmark {0}",
                            bookmarkName);

                    case BookmarkResumptionResult.NotReady:
                        HttpExceptionHelper.WriteThread(
                            "Workflow Instance {0} was not ready to receive bookmark {1} retrying...",
                            bookmarkName,
                            this.workflowApplication.Id);

                        // The workflow was not ready to receive the resumption
                        Thread.Sleep(retryMsecs);
                        timeLeft = timeLeft - TimeSpan.FromMilliseconds(retryMsecs);
                        retryMsecs = retryMsecs * 2;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            throw HttpExceptionHelper.CreateResponseException(
                HttpStatusCode.InternalServerError,
                "Unable to deliver message to Workflow ID {0} for bookmark {1}",
                this.workflowApplication.Id,
                bookmarkName);
        }

        /// <summary>
        /// The run until bookmark.
        /// </summary>
        /// <param name="bookmarkName">
        /// The bookmark name.
        /// </param>
        /// <exception cref="HttpResponseException">
        /// Timeout while waiting for bookmark
        /// </exception>
        private void RunUntilBookmark(string bookmarkName)
        {
            this.pendingBookmark = bookmarkName;
            this.workflowApplication.Run();

            if (!this.protocolEvent.WaitOne(this.serviceHost.WorkflowTimeout))
            {
                throw HttpExceptionHelper.CreateResponseException(
                    HttpStatusCode.InternalServerError,
                    "Timeout waiting for idle with bookmark {0}",
                    this.pendingBookmark);
            }
        }

        /// <summary>
        /// The load or create workflow instance.
        /// </summary>
        private void LoadOrCreateWorkflowInstance()
        {
            // Look for correlation info in the request
            var instanceId = CorrelateRequest(this.Request);

            if (instanceId != default(Guid))
            {
                this.GetFromCache(instanceId);
            }

            if (this.workflowApplication == null)
            {
                this.CreateWorkflowApplication();

                // This may fail because the workflow may not be persisted
                // TODO: What if you can't load it?
                if (instanceId != default(Guid) && this.workflowApplication.InstanceStore != null)
                {
                    this.workflowApplication.Load(instanceId);
                }

                // Once you access the ID you cannot load the Workflow application
                WorkflowApplicationCache.Add(this.workflowApplication.Id, this.workflowApplication);
            }
        }

        /// <summary>
        ///   The workflow application cache.
        /// </summary>
        private static readonly Dictionary<Guid, WorkflowApplication> WorkflowApplicationCache =
            new Dictionary<Guid, WorkflowApplication>();

        /// <summary>
        ///   The response event.
        /// </summary>
        private readonly AutoResetEvent responseEvent = new AutoResetEvent(false);

        /// <summary>
        ///   The idle.
        /// </summary>
        private bool idle;

        /// <summary>
        /// The get from cache.
        /// </summary>
        /// <param name="instanceId">
        /// The instance id.
        /// </param>
        private void GetFromCache(Guid instanceId)
        {
            lock (WorkflowApplicationCache)
            {
                WorkflowApplication application;
                if (WorkflowApplicationCache.TryGetValue(instanceId, out application))
                {
                    this.workflowApplication = application;
                    this.workflowApplication.Idle = this.OnIdle;
                }
            }
        }

        /// <summary>
        /// The create workflow application.
        /// </summary>
        private void CreateWorkflowApplication()
        {
            this.workflowApplication = new WorkflowApplication(this.ServiceHost.Activity)
                {
                    Idle = this.OnIdle,
                    Completed = this.OnCompleted
                };
            this.workflowApplication.Extensions.Add(this);
            this.workflowApplication.Extensions.Add(this.ServiceHost);
            this.workflowApplication.Extensions.Add(new InstanceTracker(this));
#if DEBUG
            this.workflowApplication.Extensions.Add(new TraceTrackingParticipant());
#endif

            if (this.ServiceHost.Extensions != null)
            {
                foreach (var extension in this.ServiceHost.Extensions)
                {
                    this.workflowApplication.Extensions.Add(extension);
                }
            }

            if (this.ServiceHost.InstanceStore != null)
            {
                this.workflowApplication.InstanceStore = this.ServiceHost.InstanceStore;
            }

            if (this.serviceHost.OnCreate != null)
            {
                this.serviceHost.OnCreate(this.workflowApplication);
            }
        }

        /// <summary>
        /// The on completed.
        /// </summary>
        /// <param name="completedEventArgs">
        /// The completed event args.
        /// </param>
        private void OnCompleted(WorkflowApplicationCompletedEventArgs completedEventArgs)
        {
            // If the workflow completed while awaiting a response, set the event
            RemoveFromCache(completedEventArgs.InstanceId);
            if (completedEventArgs.TerminationException != null)
            {
                this.terminationException = completedEventArgs.TerminationException;
            }

            this.responseEvent.Set();
        }

        private Exception terminationException;

        /// <summary>
        /// The remove from cache.
        /// </summary>
        /// <param name="instanceId">
        /// The instance id.
        /// </param>
        private static void RemoveFromCache(Guid instanceId)
        {
            lock (WorkflowApplicationCache)
            {
                WorkflowApplicationCache.Remove(instanceId);
            }
        }

        /// <summary>
        /// The on idle.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private void OnIdle(WorkflowApplicationIdleEventArgs args)
        {
            HttpExceptionHelper.WriteThread(
                "OnWorkflowIdle waiting for bookmark {0} instance Id {1} with bookmarks {2}",
                this.pendingBookmark,
                args.InstanceId,
                args.BookmarksToDelimitedList());

            this.Idle = true;

            if (args.ContainsBookmark(this.pendingBookmark))
            {
                this.pendingBookmark = null;
                this.protocolEvent.Set();
            }
        }

        /// <summary>
        /// The correlate request.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <returns>
        /// </returns>
        private static Guid CorrelateRequest(HttpRequestMessage request)
        {
            // TODO: Correlation via other means
            return WorkflowCookieCorrelation.FromRequest(request);
        }

        /// <summary>
        /// The get instance from cookie.
        /// </summary>
        /// <param name="cookie">
        /// The cookie.
        /// </param>
        /// <returns>
        /// </returns>
        private static Guid GetInstanceFromCookie(string cookie)
        {
            var parts = cookie.Split('=');
            if (parts.Length == 2)
            {
                Guid guid;
                Guid.TryParse(parts[1], out guid);
                return guid;
            }

            return Guid.Empty;
        }

        #endregion

        #region Implemented Interfaces

        #region IHttpWorkflowReceiveContext

        /// <summary>
        /// The match request uri.
        /// </summary>
        /// <returns>
        /// </returns>
        public UriTemplateMatch MatchRequestUri()
        {
            // Get a Uri template match for the current request
            return this.serviceHost.MatchSingle(this.Request);
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// The select matching receive activity.
        /// </summary>
        /// <returns>
        /// </returns>
        private HttpReceive SelectMatchingReceiveActivity()
        {
            var match = this.serviceHost.MatchSingle(this.Request);

            if (match == null)
            {
                HttpExceptionHelper.CreateResponseException(
                    HttpStatusCode.NotFound,
                    "No Http Receive activity found with Uri template matching Uri {0}",
                    this.Request.RequestUri);
            }

            return match != null ? (HttpReceive)match.Data : null;
        }

        #endregion

        /// <summary>
        /// The instance tracker.
        /// </summary>
        private class InstanceTracker : TrackingParticipant
        {
            #region Constants and Fields

            /// <summary>
            ///   The instance.
            /// </summary>
            private readonly HttpWorkflowResource instance;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="InstanceTracker"/> class.
            /// </summary>
            /// <param name="instance">
            /// The instance.
            /// </param>
            public InstanceTracker(HttpWorkflowResource instance)
            {
                this.instance = instance;
            }

            #endregion

            #region Methods

            /// <summary>
            /// The track.
            /// </summary>
            /// <param name="record">
            /// The record.
            /// </param>
            /// <param name="timeout">
            /// The timeout.
            /// </param>
            protected override void Track(TrackingRecord record, TimeSpan timeout)
            {
                if (this.instance.Idle)
                {
                    // Anything other than a WorkflowInstance record indicates activity
                    if (!(record is WorkflowInstanceRecord))
                    {
                        this.instance.Idle = false;
                    }
                }
            }

            #endregion
        }

        /// <summary>
        ///   Gets or sets a value indicating whether Idle.
        /// </summary>
        protected bool Idle
        {
            get
            {
                return this.idle;
            }

            set
            {
                this.idle = value;
                if (this.idle)
                {
                    if (this.ServiceHost.IdleSettings.UnloadOnIdle)
                    {
                        HttpExceptionHelper.WriteThread(
                            "Workflow is Idle, starting timer to unload in {0} at {1}",
                            this.serviceHost.IdleSettings.TimeToUnload.TotalSeconds,
                            DateTime.Now.ToString("mm:ss"));

                        // Start idle timer
                        this.unloadTimer = new Timer(
                            this.UnloadIfIdle,
                            null,
                            (int)this.serviceHost.IdleSettings.TimeToUnload.TotalMilliseconds,
                            -1);
                    }
                }
                else
                {
                    // Stop idle timer
                    if (this.unloadTimer != null)
                    {
                        HttpExceptionHelper.WriteThread("Workflow is no longer Idle, disposing timer ");
                        this.unloadTimer.Dispose();
                    }
                }
            }
        }

        /// <summary>
        ///   The unload timer.
        /// </summary>
        private Timer unloadTimer;

        /// <summary>
        ///   The workflow application.
        /// </summary>
        private WorkflowApplication workflowApplication;

        /// <summary>
        ///   The pending bookmark.
        /// </summary>
        private string pendingBookmark;

        /// <summary>
        ///   The protocol event.
        /// </summary>
        private readonly AutoResetEvent protocolEvent = new AutoResetEvent(false);

        /// <summary>
        /// The unload if idle.
        /// </summary>
        /// <param name="state">
        /// The state.
        /// </param>
        private void UnloadIfIdle(object state)
        {
            if (this.idle)
            {
                HttpExceptionHelper.WriteThread(
                    "Unloading Idle Instance {0} at {1}", this.workflowApplication.Id, DateTime.Now.ToString("mm:ss"));
                if (this.serviceHost.OnUnload != null)
                {
                    // The host may decide it does not want to unload this instance.
                    if (!this.serviceHost.OnUnload(this.workflowApplication))
                    {
                        HttpExceptionHelper.WriteThread(
                            "Host OnUnload returned false for Instance {0}", this.workflowApplication.Id);
                        return;
                    }
                }

                // Once unloaded you cannot use the instance again
                WorkflowApplicationCache.Remove(this.workflowApplication.Id);
                this.workflowApplication.Unload();
            }
        }
    }
}