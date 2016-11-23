// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ValidateKey.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CannonicalWorkflowHttpWebApp.Activities
{
    using System.Activities;
    using System.Net;

    using CannonicalWorkflowHttpWebApp.Infrastructure;

    using Microsoft.ApplicationServer.Http.Dispatcher;

    /// <summary>
    /// Validates a key passed to an HttpReceive activity
    /// </summary>
    /// <remarks>
    /// If the key is invalid will throw an HttpResponseException
    /// </remarks>
    public sealed class ValidateKey : CodeActivity
    {
        #region Public Properties

        /// <summary>
        ///   Gets or sets Key.
        /// </summary>
        public InArgument<string> Key { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        protected override void Execute(CodeActivityContext context)
        {
            var key = this.Key.Get(context);

            RequestValidator.Validate(key);
            ParseResourceKey(key);
        }

        /// <summary>
        /// The parse resource key.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// The parse resource key.
        /// </returns>
        /// <exception cref="HttpResponseException">
        /// </exception>
        private static int ParseResourceKey(string key)
        {
            int resourceKey;
            if (!int.TryParse(key, out resourceKey))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            return resourceKey;
        }

        #endregion
    }
}