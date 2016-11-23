// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpImports.cs" company="">
//   
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Design
{
    using System.Activities;

    using Microsoft.VisualBasic.Activities;

    /// <summary>
    /// The http imports.
    /// </summary>
    internal class HttpImports
    {
        #region Methods

        /// <summary>
        /// The add imports.
        /// </summary>
        /// <param name="activity">
        /// The activity.
        /// </param>
        /// <returns>
        /// </returns>
        internal static Activity AddImports(Activity activity)
        {
            var settings = new VisualBasicSettings();
            settings.ImportReferences.Add(
                new VisualBasicImportReference
                    {
                       Assembly = "Microsoft.ApplicationServer.Http", Import = "Microsoft.ApplicationServer.Http" 
                    });
            settings.ImportReferences.Add(
                new VisualBasicImportReference
                    {
                        Assembly = "Microsoft.ApplicationServer.Http", 
                        Import = "Microsoft.ApplicationServer.Http.Dispatcher"
                    });
            settings.ImportReferences.Add(
                new VisualBasicImportReference { Assembly = "System", Import = "System.Net" });
            settings.ImportReferences.Add(
                new VisualBasicImportReference { Assembly = "Microsoft.Net.Http", Import = "System.Net.Http" });
            VisualBasic.SetSettings(activity, settings);
            return activity;
        }

        #endregion
    }
}