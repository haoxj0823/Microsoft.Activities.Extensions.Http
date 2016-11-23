// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Customer.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Tests
{
    using System.Collections.Generic;

    /// <summary>
    /// Test customer class
    /// </summary>
    public class Customer
    {
        #region Public Properties

        /// <summary>
        ///   Gets or sets Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///   Gets or sets Name.
        /// </summary>
        public string Name { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// The get default customer list.
        /// </summary>
        /// <returns>
        /// A pre-populated customer list
        /// </returns>
        public static List<Customer> GetDefaultCustomerList()
        {
            return new List<Customer> 
            {
                    new Customer { Id = 1, Name = "Jacobs, Ron" }, 
                    new Customer { Id = 2, Name = "Hansen, Claus" }, 
                    new Customer { Id = 3, Name = "Petchdenlarp, Wirote" }, 
                    new Customer { Id = 4, Name = "Alexander, Michelle" }, 
                    new Customer { Id = 5, Name = "Andersen, Mary Kay" }, 
                };
        }

        #endregion
    }
}