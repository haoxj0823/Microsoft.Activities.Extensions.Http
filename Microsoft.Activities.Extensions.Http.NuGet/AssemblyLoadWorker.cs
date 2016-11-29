// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssemblyLoadWorker.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Extensions.Http.NuGet
{
    using System;
    using System.Reflection;

    /// <summary>
    /// The assembly load worker.
    /// </summary>
    [Serializable]
    public class AssemblyLoadWorker : MarshalByRefObject
    {
        #region Public Methods and Operators

        /// <summary>
        /// The get full name.
        /// </summary>
        /// <param name="assemblyPath">
        /// The assembly path.
        /// </param>
        /// <param name="activityName">
        /// The activity name.
        /// </param>
        /// <returns>
        /// The get full name.
        /// </returns>
        public string GetFullName(string assemblyPath, string activityName)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);

            var activityType = assembly.GetType(activityName);

            return activityType.AssemblyQualifiedName;
        }

        #endregion
    }
}