// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SampleResourceRepository.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CannonicalWorkflowHttpWebApp.Infrastructure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;

    using CannonicalWorkflowHttpWebApp.Models;

    using Microsoft.ApplicationServer.Http.Dispatcher;

    /// <summary>
    /// The sample resource repository.
    /// </summary>
    public class SampleResourceRepository : IResourceRepository<int, Sample>
    {
        #region Constants and Fields

        /// <summary>
        ///   The repository.
        /// </summary>
        private readonly ConcurrentDictionary<int, Sample> repository = new ConcurrentDictionary<int, Sample>();

        /// <summary>
        ///   The current resource repository.
        /// </summary>
        private static SampleResourceRepository currentRepository = Initialize();

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets the Current repository.
        /// </summary>
        public static SampleResourceRepository Current
        {
            get
            {
                return currentRepository;
            }
        }

        /// <summary>
        ///   Gets Resources.
        /// </summary>
        public IList<Sample> Resources
        {
            get
            {
                return new ReadOnlyCollection<Sample>(this.repository.Values.ToList());
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The get.
        /// </summary>
        /// <returns>
        /// The sample resource
        /// </returns>
        public static SampleResourceRepository Initialize()
        {
            var resourceRepository = new SampleResourceRepository();

            for (var i = 1; i <= 20; i++)
            {
                resourceRepository.repository.AddOrUpdate(
                    i, new Sample { Key = i, Data = "HttpResource" + i }, (key, existing) => existing);
            }

            currentRepository = resourceRepository;
            return resourceRepository;
        }

        /// <summary>
        /// The add or update.
        /// </summary>
        /// <param name="resourceKey">
        /// The resource key.
        /// </param>
        /// <param name="resource">
        /// The resource.
        /// </param>
        /// <param name="onAdd">
        /// The on add.
        /// </param>
        /// <param name="onUpdate">
        /// The on update.
        /// </param>
        /// <returns>
        /// Adds or updates a resource
        /// </returns>
        public Sample AddOrUpdate(int resourceKey, Sample resource, Action<Sample> onAdd, Action<Sample> onUpdate)
        {
            return this.repository.AddOrUpdate(
                resourceKey, 
                key =>
                    {
                        // If resource was not found
                        // This delegate will return a resource to add to the store
                        var sanitizedResource = Sample.CreateSanitizedResource(
                            key, resource, SampleResourceVersionOption.New);

                        if (onAdd != null)
                        {
                            onAdd(sanitizedResource);
                        }

                        return sanitizedResource;
                    }, 
                (key, existingResource) =>
                    {
                        // If the resource was found
                        // This delegate will update the resource found based on the caller provided resource
                        if (onUpdate != null)
                        {
                            onUpdate(existingResource);
                        }

                        var sanitizedResource = Sample.CreateSanitizedResource(
                            key, resource, SampleResourceVersionOption.UseExisting);

                        // Because PUT requests are Idempotent (multiple calls yield the same result)
                        // Don't change the version of the resource unless the data is really changed
                        return existingResource.UpdateFrom(sanitizedResource);
                    });
        }

        /// <summary>
        /// The delete.
        /// </summary>
        /// <param name="resourceKey">
        /// The resource key.
        /// </param>
        /// <param name="checkPreCondition">
        /// The check pre condition.
        /// </param>
        /// <returns>
        /// The resource that was deleted
        /// </returns>
        public Sample Delete(int resourceKey, Action<Sample> checkPreCondition)
        {
            // Need to Get, Check PreCondition and Remove in one atomic operation
            lock (this.repository)
            {
                var resource = this.Get(resourceKey);

                if (resource != null)
                {
                    // Will throw if pre-condition fails
                    checkPreCondition(resource);

                    this.repository.TryRemove(resourceKey, out resource);
                }

                return resource;
            }
        }

        /// <summary>
        /// The get.
        /// </summary>
        /// <param name="resourceKey">
        /// The resource key.
        /// </param>
        /// <returns>
        /// The Sample resource
        /// </returns>
        public Sample Get(int resourceKey)
        {
            Sample result;
            this.repository.TryGetValue(resourceKey, out result);

            // If not found, returns null
            return result;
        }

        /// <summary>
        /// The get resources.
        /// </summary>
        /// <param name="skip">
        /// The skip.
        /// </param>
        /// <param name="take">
        /// The take.
        /// </param>
        /// <returns>
        /// An array of sample resources
        /// </returns>
        public Sample[] GetResources(int skip, int take)
        {
            return this.repository.Values.Skip(skip).Take(take).ToArray();
        }

        /// <summary>
        /// The post.
        /// </summary>
        /// <param name="sample">
        /// The sample.
        /// </param>
        /// <returns>
        /// A sample resource
        /// </returns>
        /// <exception cref="HttpResponseException">
        /// The resource is in conflict or could not be added
        /// </exception>
        public Sample Post(Sample sample)
        {
            // Sanitize the data provided by the caller using the version the caller supplied
            var sanitizedResource = Sample.CreateSanitizedResource(
                this.GenerateId(), sample, SampleResourceVersionOption.New);

            // Check to see if the resource that is being added has a conflict with an existing resource
            // For example, you might not allow the same email address more than once.
            if (this.ResourceConflict(sanitizedResource))
            {
                throw new HttpResponseException(HttpStatusCode.Conflict);
            }

            if (this.repository.TryAdd(sanitizedResource.Key, sanitizedResource))
            {
                return sanitizedResource;
            }

            throw new HttpResponseException(HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// The put.
        /// </summary>
        /// <param name="resourceKey">
        /// The resource key.
        /// </param>
        /// <param name="toPut">
        /// The to put.
        /// </param>
        /// <param name="comparison">
        /// The comparison.
        /// </param>
        /// <returns>
        /// A sample resource
        /// </returns>
        public Sample Put(int resourceKey, Sample toPut, Sample comparison)
        {
            var sanitizedResource = Sample.CreateSanitizedResource(
                resourceKey, toPut, SampleResourceVersionOption.UseExisting);

            if (sanitizedResource.IsValid() && comparison.DataChanged(sanitizedResource))
            {
                if (this.repository.TryUpdate(resourceKey, sanitizedResource, comparison))
                {
                    sanitizedResource.UpdateVersion();
                    return sanitizedResource;
                }

                return null;
            }

            return sanitizedResource;
        }

        /// <summary>
        /// The resource conflict.
        /// </summary>
        /// <param name="sample">
        /// The sample.
        /// </param>
        /// <returns>
        /// True if there is a resource conflict
        /// </returns>
        public bool ResourceConflict(Sample sample)
        {
            // look for other resources with the same data
            return this.repository.Contains(
                new KeyValuePair<int, Sample>(sample.Key, sample), new SampleResourceConflictComparer());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Generates a new Id
        /// </summary>
        /// <returns>
        /// the new Id
        /// </returns>
        private int GenerateId()
        {
            return (from r in this.Resources select r.Key).Max() + 1;
        }

        #endregion
    }
}