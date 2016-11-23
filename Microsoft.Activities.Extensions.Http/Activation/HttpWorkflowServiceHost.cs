// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpWorkflowServiceHost.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Activation
{
    using System;
    using System.Activities;
    using System.Activities.Hosting;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.DurableInstancing;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Description;

    using Microsoft.Activities.Http.Activities;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Http.Dispatcher;

    /// <summary>
    /// The http workflow service host.
    /// </summary>
    public class HttpWorkflowServiceHost : HttpServiceHost, IHttpWorkflowHostContext
    {
        #region Constants and Fields

        /// <summary>
        ///   The default timeout.
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

        /// <summary>
        ///   The hosts.
        /// </summary>
        private static readonly List<HttpWorkflowServiceHost> Hosts = new List<HttpWorkflowServiceHost>();

        /// <summary>
        ///   The URI template tables.
        /// </summary>
        private static readonly Dictionary<Activity, List<UriTemplateTable>> UriTemplateTables =
            new Dictionary<Activity, List<UriTemplateTable>>();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpWorkflowServiceHost"/> class.
        /// </summary>
        /// <param name="activity">
        /// The activity.
        /// </param>
        /// <param name="baseAddresses">
        /// The base addresses.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// An activity was not supplied
        /// </exception>
        public HttpWorkflowServiceHost(Activity activity, params Uri[] baseAddresses)
            : base(typeof(HttpWorkflowResource), CreateConfiguration(), baseAddresses)
        {
            Contract.Requires(activity != null);

            if (activity == null)
            {
                throw new ArgumentNullException("activity");
            }

            this.Initialize(activity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpWorkflowServiceHost"/> class.
        /// </summary>
        /// <param name="serviceImplementation">
        /// The service implementation.
        /// </param>
        /// <param name="baseAddresses">
        /// The base addresses.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The service implementation was not supplied
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The service implementation was not an activity
        /// </exception>
        public HttpWorkflowServiceHost(object serviceImplementation, params Uri[] baseAddresses)
            : base(typeof(HttpWorkflowResource), CreateConfiguration(), baseAddresses)
        {
            Contract.Requires(serviceImplementation != null);
            Contract.Requires(serviceImplementation.GetType().IsAssignableFrom(typeof(Activity)));

            if (serviceImplementation == null)
            {
                throw new ArgumentNullException("serviceImplementation");
            }

            if (!serviceImplementation.GetType().IsAssignableFrom(typeof(Activity)))
            {
                throw new ArgumentException("serviceImplementation must be an Activity");
            }

            this.Initialize(serviceImplementation as Activity);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "HttpWorkflowServiceHost" /> class.
        /// </summary>
        protected HttpWorkflowServiceHost()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets Activity.
        /// </summary>
        public Activity Activity { get; private set; }

        /// <summary>
        ///   Gets or sets IdleSettings.
        /// </summary>
        public HttpWorkflowIdleSettings IdleSettings { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether exception details are included in faults
        /// </summary>
        public bool IncludeExceptionDetails
        {
            get
            {
                return HttpExceptionHelper.IncludeExceptionDetails;
            }

            set
            {
                HttpExceptionHelper.IncludeExceptionDetails = value;
            }
        }

        /// <summary>
        ///   Gets or sets a the instance store to use when loading or persisting workflow instances
        /// </summary>
        public InstanceStore InstanceStore { get; set; }

        /// <summary>
        ///   Gets or sets OnCreate.
        /// </summary>
        public Action<WorkflowApplication> OnCreate { get; set; }

        /// <summary>
        ///   Gets or sets OnUnload.
        /// </summary>
        public Func<WorkflowApplication, bool> OnUnload { get; set; }

        /// <summary>
        ///   Gets extensions that you want to add to the workflow
        /// </summary>
        public WorkflowInstanceExtensionManager WorkflowExtensions { get; private set; }

        /// <summary>
        ///   Gets or sets the length of time the host will wait for the workflow to complete episodes of work before responding
        /// </summary>
        public TimeSpan WorkflowTimeout { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Searches the uriTemplate tables collection for a match
        /// </summary>
        /// <param name="request">
        /// The Request URI
        /// </param>
        /// <returns>
        /// A UriTemplateMatch or null if not found
        /// </returns>
        public UriTemplateMatch MatchSingle(HttpRequestMessage request)
        {
            var uriTemplateTables = UriTemplateTables[this.Activity];
            return (from uriTemplateTable in uriTemplateTables
                    from match in uriTemplateTable.Match(request.RequestUri)
                    let receiveMethod = new HttpMethod(((HttpReceive)match.Data).Method)
                    where request.Method == receiveMethod
                    select match).FirstOrDefault();
        }

        #endregion

        #region Methods

        /// <summary>
        /// The get host.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <returns>
        /// The host matching the request
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// A matching host could not be found
        /// </exception>
        internal static HttpWorkflowServiceHost GetHost(HttpRequestMessage request)
        {
            // Find a host that has a matching Uri 
            foreach (HttpWorkflowServiceHost httpWorkflowServiceHost in Hosts)
            {
                foreach (Uri baseAddress in httpWorkflowServiceHost.BaseAddresses)
                {
                    if (request.RequestUri.ToString().StartsWith(baseAddress.ToString()))
                    {
                        return httpWorkflowServiceHost;
                    }
                }
            }

            // This should never happen
            throw new InvalidOperationException("No matching host for base URI was found");
        }

        /// <summary>
        /// The get matching base address.
        /// </summary>
        /// <param name="requestUri">
        /// The request Uri.
        /// </param>
        /// <returns>
        /// A matching URI
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// No matching base address was found for the URI
        /// </exception>
        internal Uri GetMatchingBaseAddress(Uri requestUri)
        {
            // Note: Case Sensitive Comparison
            // from http://www.w3.org/TR/WD-html40-970708/htmlweb.html
            // URLs in general are case-sensitive (with the exception of machine names). There may be URLs, or parts of URLs, where case doesn't matter, but identifying these may not be easy. Users should always consider that URLs are case-sensitive.
            foreach (var baseAddress in this.BaseAddresses)
            {
                // Because portions of the base address are not case-sensitive (machine name, scheme etc.)
                // Compare only the path
                var requestUrib = new UriBuilder(requestUri);
                var baseAddressUrib = new UriBuilder(baseAddress);
                if (requestUrib.Path.StartsWith(baseAddressUrib.Path))
                {
                    return baseAddress;
                }
            }

            throw new InvalidOperationException("No matching base address found for request URI");
        }

        /// <summary>
        /// The on closing.
        /// </summary>
        protected override void OnClosing()
        {
            Hosts.Remove(this);
            base.OnClosing();
        }

        /// <summary>
        /// The on opening.
        /// </summary>
        protected override void OnOpening()
        {
            Hosts.Add(this);
            base.OnOpening();
        }

        /// <summary>
        /// Creates the configuration for the the web API objects
        /// </summary>
        /// <returns>
        /// The configuration object
        /// </returns>
        private static HttpConfiguration CreateConfiguration()
        {
            return new HttpConfiguration
                {
                    CreateInstance = CreateWorkflowInstance, 
                    TrailingSlashMode = TrailingSlashMode.AutoRedirect
                    //// ResponseHandlers = CreateResponseHandlers,
                };
        }

        //private static void CreateResponseHandlers(Collection<HttpOperationHandler> responseHandlers, ServiceEndpoint serviceEndpoint, HttpOperationDescription operationDescription)
        //{
        //    responseHandlers.Add(new DynamicQueryHandler());
        //}

        /// <summary>
        /// The create workflow instance.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <returns>
        /// The create workflow instance.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        private static object CreateWorkflowInstance(Type type, InstanceContext context, HttpRequestMessage request)
        {
            if (type == typeof(HttpWorkflowResource))
            {
                return new HttpWorkflowResource((HttpWorkflowServiceHost)context.Host, request);
            }

            throw new InvalidOperationException("Invalid host type");
        }

        /// <summary>
        /// The initialize.
        /// </summary>
        /// <param name="activity">
        /// The activity.
        /// </param>
        private void Initialize(Activity activity)
        {
            if (this.WorkflowTimeout == TimeSpan.Zero)
            {
                this.WorkflowTimeout = DefaultTimeout;
            }

            this.IdleSettings = new HttpWorkflowIdleSettings();

            this.SetAspNetCompatabilityRequirements();
            this.Activity = activity;
            this.WorkflowExtensions = new WorkflowInstanceExtensionManager();

            this.InitializeTemplateTables();
        }

        /// <summary>
        /// The initialize template tables.
        /// </summary>
        /// <exception cref="ValidationException">
        /// The workflow is invalid
        /// </exception>
        private void InitializeTemplateTables()
        {
            try
            {
                // Cache definitions so we don't run CacheMetadata more than once.
                if (!UriTemplateTables.ContainsKey(this.Activity))
                {
                    WorkflowInspectionServices.CacheMetadata(this.Activity);
                    UriTemplateTables.Add(this.Activity, new List<UriTemplateTable>());
                    foreach (var uriTemplateTable in
                        this.BaseAddresses.Select(baseAddress => new UriTemplateTable(baseAddress)))
                    {
                        UriTemplateTables[this.Activity].Add(uriTemplateTable);
                        this.LocateHttpReceiveActivities(this.Activity, uriTemplateTable);

                        // No UriTemplates in this activity
                        if (uriTemplateTable.KeyValuePairs.Count == 0)
                        {
                            throw new ValidationException(
                                "Activity must contain at least one HttpReceive activity with a valid Uri template");
                        }
                    }
                }
            }

#if DEBUG
            catch (Exception ex)
            {
                HttpExceptionHelper.WriteThread(ex.Message);
                throw;
            }

#endif
        }

        /// <summary>
        /// The locate http receive activities.
        /// </summary>
        /// <param name="activity">
        /// The activity.
        /// </param>
        /// <param name="uriTemplateTable">
        /// The uri template table.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The activity is null
        /// </exception>
        private void LocateHttpReceiveActivities(Activity activity, UriTemplateTable uriTemplateTable)
        {
            if (activity == null)
            {
                throw new ArgumentNullException("activity");
            }

            if (activity is HttpReceive)
            {
                var receive = (HttpReceive)activity;

                uriTemplateTable.KeyValuePairs.Add(
                    new KeyValuePair<UriTemplate, object>(new UriTemplate(receive.UriTemplate), receive));
            }

            foreach (var childActivity in WorkflowInspectionServices.GetActivities(activity))
            {
                this.LocateHttpReceiveActivities(childActivity, uriTemplateTable);
            }
        }

        /// <summary>
        /// The set asp net compatibility requirements.
        /// </summary>
        private void SetAspNetCompatabilityRequirements()
        {
            this.Description.Behaviors.Remove<AspNetCompatibilityRequirementsAttribute>();
            var item = new AspNetCompatibilityRequirementsAttribute
                {
                   RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed 
                };
            this.Description.Behaviors.Add(item);
        }

        #endregion
    }
}