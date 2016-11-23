// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ValidateKey.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CannonicalWorkflowHttpWebApp.Infrastructure
{
    using System;
    using System.Net;
    using System.Net.Http;

    using CannonicalWorkflowHttpWebApp.Models;

    using Microsoft.ApplicationServer.Http.Dispatcher;

    /// <summary>
    /// The request validator.
    /// </summary>
    internal static class RequestValidator
    {
        #region Public Methods

        /// <summary>
        /// The is valid key.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// true if the key is valid
        /// </returns>
        public static bool IsValidKey(int key)
        {
            return key >= 0;
        }

        /// <summary>
        /// The validate.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        public static void Validate(int key)
        {
            ValidateRequest(IsValidKey, key, "Invalid key");
        }

        /// <summary>
        /// The validate.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="resource">
        /// The resource.
        /// </param>
        public static void Validate(int key, Sample resource)
        {
            Validate(key);
            Validate(resource);
        }

        #endregion

        #region Methods

        /// <summary>
        /// The is positive.
        /// </summary>
        /// <param name="number">
        /// The number.
        /// </param>
        /// <returns>
        /// true if the number is positive
        /// </returns>
        internal static bool IsPositive(int number)
        {
            return number >= 0;
        }

        /// <summary>
        /// The is valid key.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// true if the key is valid
        /// </returns>
        internal static bool IsValidKey(string key)
        {
            return !string.IsNullOrWhiteSpace(key);
        }

        /// <summary>
        /// The is valid resource.
        /// </summary>
        /// <param name="sample">
        /// The sample.
        /// </param>
        /// <returns>
        /// true if the resource is valid
        /// </returns>
        internal static bool IsValidResource(Sample sample)
        {
            return sample != null && sample.IsValid();
        }

        /// <summary>
        /// The is valid skip.
        /// </summary>
        /// <param name="skip">
        /// The skip.
        /// </param>
        internal static void IsValidSkip(int skip)
        {
            ValidateRequest(IsPositive, skip, "Invalid skip value {0}", skip);
        }

        /// <summary>
        /// The is valid take.
        /// </summary>
        /// <param name="take">
        /// The take.
        /// </param>
        internal static void IsValidTake(int take)
        {
            ValidateRequest(IsPositive, take, "Invalid take value {0}", take);
        }

        /// <summary>
        /// The validate.
        /// </summary>
        /// <param name="sample">
        /// The sample.
        /// </param>
        internal static void Validate(Sample sample)
        {
            ValidateRequest(IsValidResource, sample, "Invalid Sample Sample");
        }

        /// <summary>
        /// The validate.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        internal static void Validate(string key)
        {
            ValidateRequest(IsValidKey, key, "Invalid key");
        }

        /// <summary>
        /// The validate.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="resource">
        /// The resource.
        /// </param>
        internal static void Validate(string key, Sample resource)
        {
            Validate(key);
            Validate(resource);
        }

        /// <summary>
        /// The validate request.
        /// </summary>
        /// <param name="isValid">
        /// The is valid.
        /// </param>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <exception cref="HttpResponseException">
        /// The request is not valid
        /// </exception>
        internal static void ValidateRequest(Func<bool> isValid, string format, params object[] args)
        {
            if (!isValid())
            {
                throw new HttpResponseException(
                    new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.BadRequest, 
                            Content = new StringContent(string.Format(format, args))
                        });
            }
        }

        /// <summary>
        /// The validate request.
        /// </summary>
        /// <param name="isValid">
        /// The is valid.
        /// </param>
        /// <param name="arg1">
        /// The arg 1.
        /// </param>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <typeparam name="T1">
        /// The type of request to validate
        /// </typeparam>
        internal static void ValidateRequest<T1>(Func<T1, bool> isValid, T1 arg1, string format, params object[] args)
        {
            ValidateRequest(() => isValid(arg1), format, args);
        }

        #endregion
    }
}