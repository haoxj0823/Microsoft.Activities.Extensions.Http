// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpWorkflowServiceTestHost.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.UnitTesting
{
    using System;
    using System.Activities;
    using System.Activities.XamlIntegration;
    using System.Threading;

    using Microsoft.Activities.Http.Activation;
    using Microsoft.Activities.UnitTesting.Persistence;
    using Microsoft.Activities.UnitTesting.Tracking;

    /// <summary>
    /// The http workflow service test host.
    /// </summary>
    public class HttpWorkflowServiceTestHost : IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// The default timeout.
        /// </summary>
        private static readonly TimeSpan DefaultTimeout =
            TimeSpan.FromSeconds(10);

        /// <summary>
        /// The create event.
        /// </summary>
        private readonly AutoResetEvent createEvent = new AutoResetEvent(false);

        /// <summary>
        /// The unload event.
        /// </summary>
        private readonly AutoResetEvent unloadEvent = new AutoResetEvent(false);

        /// <summary>
        /// The tracking.
        /// </summary>
        private MemoryTrackingParticipant tracking;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpWorkflowServiceTestHost"/> class.
        /// </summary>
        /// <param name="serviceHost">
        /// The service host.
        /// </param>
        protected HttpWorkflowServiceTestHost(
            HttpWorkflowServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.InstanceStore = new MemoryStore();
            this.ServiceHost.WorkflowTimeout = DefaultTimeout;
            this.ServiceHost.OnCreate = this.OnCreateHandler;
            this.ServiceHost.OnUnload = this.OnUnloadHandler;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets OnCreate.
        /// </summary>
        public Action<WorkflowApplication> OnCreate { get; set; }

        /// <summary>
        /// Gets or sets OnUnload.
        /// </summary>
        public Func<WorkflowApplication, bool> OnUnload { get; set; }

        /// <summary>
        /// Gets ServiceHost.
        /// </summary>
        public HttpWorkflowServiceHost ServiceHost { get; private set; }

        /// <summary>
        /// Gets or sets TestTimeout.
        /// </summary>
        public TimeSpan TestTimeout { get; set; }

        /// <summary>
        /// Gets Tracking.
        /// </summary>
        public MemoryTrackingParticipant Tracking
        {
            get
            {
                return this.tracking ??
                       (this.tracking = new MemoryTrackingParticipant());
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The open.
        /// </summary>
        /// <param name="activity">
        /// The activity.
        /// </param>
        /// <param name="baseAddress">
        /// The base address.
        /// </param>
        /// <returns>
        /// An HttpWorkflowServiceTestHost
        /// </returns>
        public static HttpWorkflowServiceTestHost Open(
            Activity activity, Uri baseAddress)
        {
            return Open(activity, baseAddress, DefaultTimeout);
        }

        /// <summary>
        /// The open.
        /// </summary>
        /// <param name="activity">
        /// The activity.
        /// </param>
        /// <param name="baseAddress">
        /// The base address.
        /// </param>
        /// <param name="timeout">
        /// The timeout.
        /// </param>
        /// <returns>
        /// An HttpWorkflowServiceTestHost
        /// </returns>
        public static HttpWorkflowServiceTestHost Open(
            Activity activity, Uri baseAddress, TimeSpan timeout)
        {
            var testhost =
                new HttpWorkflowServiceTestHost(
                    new HttpWorkflowServiceHost(activity, new[] { baseAddress }));
            testhost.ServiceHost.Open();
            testhost.TestTimeout = timeout;
            testhost.ServiceHost.WorkflowTimeout = timeout;
            return testhost;
        }

        /// <summary>
        /// The open.
        /// </summary>
        /// <param name="file">
        /// The file.
        /// </param>
        /// <param name="baseAddress">
        /// The base address.
        /// </param>
        /// <returns>
        /// An HttpWorkflowServiceTestHost
        /// </returns>
        public static HttpWorkflowServiceTestHost Open(
            string file, Uri baseAddress)
        {
            return Open(file, baseAddress, DefaultTimeout);
        }

        /// <summary>
        /// The open.
        /// </summary>
        /// <param name="file">
        /// The file.
        /// </param>
        /// <param name="baseAddress">
        /// The base address.
        /// </param>
        /// <param name="timeout">
        /// The timeout.
        /// </param>
        /// <returns>
        /// An HttpWorkflowServiceTestHost
        /// </returns>
        public static HttpWorkflowServiceTestHost Open(
            string file, Uri baseAddress, TimeSpan timeout)
        {
            return Open(ActivityXamlServices.Load(file), baseAddress, timeout);
        }

        /// <summary>
        /// The close.
        /// </summary>
        public void Close()
        {
            if (this.ServiceHost != null)
            {
                this.ServiceHost.Close();
            }
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Close();
        }

        /// <summary>
        /// The try wait for unload.
        /// </summary>
        /// <returns>
        /// True if the unload event occurred
        /// </returns>
        public bool TryWaitForUnload()
        {
            return this.unloadEvent.WaitOne(this.TestTimeout);
        }

        /// <summary>
        /// The try wait for unload.
        /// </summary>
        /// <param name="timeout">
        /// The timeout.
        /// </param>
        /// <returns>
        /// True if the unload event occurred
        /// </returns>
        public bool TryWaitForUnload(TimeSpan timeout)
        {
            return this.unloadEvent.WaitOne(timeout);
        }

        /// <summary>
        /// The wait for unload.
        /// </summary>
        /// <exception cref="TimeoutException">
        /// The unload event did not occur within the timeout
        /// </exception>
        public void WaitForUnload()
        {
            if (!this.unloadEvent.WaitOne(this.TestTimeout))
            {
                HttpExceptionHelper.WriteThread(
                    "Timeout waiting for workflow to unload");
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// The wait for unload.
        /// </summary>
        /// <param name="timeout">
        /// The timeout.
        /// </param>
        /// <exception cref="TimeoutException">
        /// The unload event did not occur within the timeout
        /// </exception>
        public void WaitForUnload(TimeSpan timeout)
        {
            if (!this.unloadEvent.WaitOne(timeout))
            {
                HttpExceptionHelper.WriteThread(
                    "Timeout waiting for workflow to unload");
                throw new TimeoutException();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The on create handler.
        /// </summary>
        /// <param name="arg">
        /// The arg.
        /// </param>
        private void OnCreateHandler(WorkflowApplication arg)
        {
            if (this.OnCreate != null)
            {
                this.OnCreate(arg);
            }

            this.createEvent.Set();
        }

        /// <summary>
        /// The on unload handler.
        /// </summary>
        /// <param name="arg">
        /// The arg.
        /// </param>
        /// <returns>
        /// The unload handler result (if provided)
        /// </returns>
        private bool OnUnloadHandler(WorkflowApplication arg)
        {
            try
            {
                if (this.OnUnload != null)
                {
                    return this.OnUnload(arg);
                }

                return true;
            }
            finally
            {
                this.unloadEvent.Set();
            }
        }

        #endregion
    }
}