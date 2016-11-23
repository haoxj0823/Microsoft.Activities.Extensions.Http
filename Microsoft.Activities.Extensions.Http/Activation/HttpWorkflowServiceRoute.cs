// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpWorkflowServiceRoute.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Activation
{
    using System.Activities;
    using System.Reflection;
    using System.ServiceModel.Activation;
    using System.Web.Routing;

    /// <summary>
    /// Provides routing support for Workflow Services
    /// </summary>
    public class HttpWorkflowServiceRoute : ServiceRoute
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpWorkflowServiceRoute"/> class.
        /// </summary>
        /// <param name="routePrefix">
        /// The route prefix.
        /// </param>
        /// <param name="workflowFile">
        /// The workflow file.
        /// </param>
        /// <param name="localAssembly">
        /// The local assembly.
        /// </param>
        public HttpWorkflowServiceRoute(string routePrefix, string workflowFile, Assembly localAssembly)
            : base(
                routePrefix, 
                new HttpWorkflowServiceHostFactory(workflowFile, localAssembly), 
                typeof(HttpWorkflowResource))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpWorkflowServiceRoute"/> class.
        /// </summary>
        /// <param name="routePrefix">
        /// The route prefix.
        /// </param>
        /// <param name="activity">
        /// The activity.
        /// </param>
        public HttpWorkflowServiceRoute(string routePrefix, Activity activity)
            : base(routePrefix, new HttpWorkflowServiceHostFactory(activity), typeof(HttpWorkflowResource))
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The get virtual path.
        /// </summary>
        /// <param name="requestContext">
        /// The request context.
        /// </param>
        /// <param name="values">
        /// The values.
        /// </param>
        /// <returns>
        /// null
        /// </returns>
        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            return null;
        }

        #endregion
    }
}