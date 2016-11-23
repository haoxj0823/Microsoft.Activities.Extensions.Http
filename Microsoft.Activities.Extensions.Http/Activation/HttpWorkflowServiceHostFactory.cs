// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpWorkflowServiceHostFactory.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Activation
{
    using System;
    using System.Activities;
    using System.Activities.XamlIntegration;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.Xaml;

    /// <summary>
    /// Provides a factory for creating an HttpWorkflowServiceHost
    /// </summary>
    public class HttpWorkflowServiceHostFactory : ServiceHostFactoryBase
    {
        #region Constants and Fields

        /// <summary>
        /// The activity.
        /// </summary>
        private readonly Activity activity;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpWorkflowServiceHostFactory"/> class.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <param name="localAssembly">
        /// The local assembly.
        /// </param>
        public HttpWorkflowServiceHostFactory(string path, Assembly localAssembly)
        {
            this.activity = localAssembly != null
                                ? ActivityXamlServices.Load(
                                    ActivityXamlServices.CreateReader(
                                        new XamlXmlReader(
                                      path, new XamlXmlReaderSettings { LocalAssembly = localAssembly })))
                                : ActivityXamlServices.Load(path);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpWorkflowServiceHostFactory"/> class.
        /// </summary>
        /// <param name="activity">
        /// The activity.
        /// </param>
        public HttpWorkflowServiceHostFactory(Activity activity)
        {
            this.activity = activity;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The create service host.
        /// </summary>
        /// <param name="constructorString">
        /// The constructor string.
        /// </param>
        /// <param name="baseAddresses">
        /// The base addresses.
        /// </param>
        /// <returns>
        /// </returns>
        public override ServiceHostBase CreateServiceHost(string constructorString, Uri[] baseAddresses)
        {
            return new HttpWorkflowServiceHost(this.activity, baseAddresses);
        }

        #endregion
    }
}