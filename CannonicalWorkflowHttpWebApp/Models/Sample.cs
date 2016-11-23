// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Sample.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CannonicalWorkflowHttpWebApp.Models
{
    using System;

    using CannonicalWorkflowHttpWebApp.Infrastructure;

    /// <summary>
    /// Sample model
    /// </summary>
    /// <remarks>
    /// HTTP does not define how you should deal with a resource where a portion of the entity is not writable.  The generally accepted practice is to ignore fields that cannot be written
    /// </remarks>
    public class Sample
    {
        // Note: Media types are declared here in the event that we may want to use a custom media type in the future.
        #region Constants and Fields

        /// <summary>
        /// The json media type.
        /// </summary>
        public const string JsonMediaType = "application/json";

        /// <summary>
        /// The xml media type.
        /// </summary>
        public const string XmlMediaType = "application/xml";

        /// <summary>
        ///   Read only value
        /// </summary>
        /// <remarks>
        ///   Note: Don't be lulled into a false sense of security by the  readonly attribute on this field. Deserialization will happily write to it. You must protect it with defensive coding
        /// </remarks>
        public readonly string ReadOnlyData = "This is read only data";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Sample"/> class. 
        ///   Initializes a new Sample model
        /// </summary>
        public Sample()
        {
            this.Tag = CreateNewETag();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets Data.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets Key.
        /// </summary>
        public int Key { get; set; }

        /// <summary>
        /// Gets or sets Tag.
        /// </summary>
        public string Tag { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a sanitized updatedResource based on one supplied by the caller
        /// </summary>
        /// <param name="id">
        /// The id of the updatedResource to create
        /// </param>
        /// <param name="untrusted">
        /// The resource supplied by the caller
        /// </param>
        /// <param name="versionOption">
        /// The version option
        /// </param>
        /// <returns>
        /// A new resource based on the untrusted resource
        /// </returns>
        /// <remarks>
        /// Sanitizing a resource means converting an untrusted resource
        ///   into something you can trust.
        /// </remarks>
        internal static Sample CreateSanitizedResource(
            int id, Sample untrusted, SampleResourceVersionOption versionOption)
        {
            // Return a new sanitized updatedResource with only the changes allowed
            return new Sample
                {
                    Key = id, 
                    Data = untrusted.Data, 
                    Tag = (versionOption == SampleResourceVersionOption.New) ? Guid.NewGuid().ToString() : untrusted.Tag, 

                    // ReadOnlyData is not initialized because we don't allow the caller to update it
                };
        }

        /// <summary>
        /// The data changed.
        /// </summary>
        /// <param name="update">
        /// The update.
        /// </param>
        /// <returns>
        /// true if the data is changed
        /// </returns>
        internal bool DataChanged(Sample update)
        {
            return this.Data != update.Data;
        }

        /// <summary>
        /// The is same version as.
        /// </summary>
        /// <param name="update">
        /// The update.
        /// </param>
        /// <returns>
        /// true if the version is the same
        /// </returns>
        internal bool IsSameVersionAs(Sample update)
        {
            return this.Tag == update.Tag;
        }

        /// <summary>
        /// Determines if a updatedResource is valid
        /// </summary>
        /// <returns>
        /// The is valid.
        /// </returns>
        internal bool IsValid()
        {
            // Don't validate ID here - callers can't modify it
            // Only validate things callers can modify
            return !string.IsNullOrWhiteSpace(this.Data);
        }

        /// <summary>
        /// Updates the updatedResource and version if values have changed
        /// </summary>
        /// <param name="update">
        /// The updatedResource you are comparing to
        /// </param>
        /// <returns>
        /// The updated resource
        /// </returns>
        internal Sample UpdateFrom(Sample update)
        {
            if (this.IsSameVersionAs(update))
            {
                if (update.IsValid() && this.DataChanged(update))
                {
                    // Update only the fields you allow
                    // the caller to update
                    this.Data = update.Data;

                    this.UpdateVersion();
                }
            }

            // Ignore fields you don't want to allow the caller to change
            // Ignore updatedResource.ReadOnlyValue
            // Ignore updatedResource.Version
            return this;
        }

        /// <summary>
        /// The update version.
        /// </summary>
        internal void UpdateVersion()
        {
            this.Tag = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Creates a new ETag
        /// </summary>
        /// <returns>
        /// The new Etag
        /// </returns>
        private static string CreateNewETag()
        {
            return Guid.NewGuid().ToString();
        }

        #endregion
    }
}