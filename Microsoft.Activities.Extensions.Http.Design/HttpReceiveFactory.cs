// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpReceiveFactory.cs" company="">
//   
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Design
{
    using System.Activities;
    using System.Activities.Presentation;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Windows;

    using Microsoft.Activities.Http.Activities;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class HttpReceiveFactory : IActivityTemplateFactory
    {
        #region Public Methods

        /// <summary>
        /// Creates an HttpReceive.
        /// </summary>
        /// <returns>
        /// An activity
        /// </returns>
        public static Activity Create()
        {
            return
                HttpImports.AddImports(
                    new HttpReceive
                        {
                            UriTemplate = "/{ID}", 
                            CanCreateInstance = true, 
                            Body =
                                new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object> 
                                {
                                        Argument1 = new DelegateInArgument<HttpRequestMessage>("request"), 
                                        Argument2 = new DelegateInArgument<IDictionary<string, string>>("args"), 
                                        Result = new DelegateOutArgument<object>("response"), 
                                    }
                        });
        }

        /// <summary>
        /// The create.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// </returns>
        public Activity Create(DependencyObject target)
        {
            return Create();
        }

        #endregion
    }

    /// <summary>
    /// The http workflow service factory.
    /// </summary>
    public class HttpWorkflowServiceFactory : IActivityTemplateFactory
    {
        #region Public Methods

        /// <summary>
        /// The create.
        /// </summary>
        /// <returns>
        /// </returns>
        public static Activity Create()
        {
            return HttpImports.AddImports(new HttpWorkflowService());
        }

        /// <summary>
        /// The create.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// </returns>
        public Activity Create(DependencyObject target)
        {
            return Create();
        }

        #endregion
    }
}