// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Global.asax.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CannonicalWorkflowHttpWebApp
{
    using System;
    using System.Reflection;
    using System.Web;
    using System.Web.Routing;

    using Microsoft.Activities.Http.Activation;

    /// <summary>
    /// The global.
    /// </summary>
    public class Global : HttpApplication
    {
        #region Methods

        /// <summary>
        /// The application_ start.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected void Application_Start(object sender, EventArgs e)
        {
            // Route requests to /api to the SampleResource.xaml workflow
            RouteTable.Routes.Add(
                new HttpWorkflowServiceRoute("api", this.Server.MapPath("~/XAML/SampleResource.xaml"), Assembly.GetExecutingAssembly()));
        }

        #endregion
    }
}