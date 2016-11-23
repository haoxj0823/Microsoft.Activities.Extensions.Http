// -----------------------------------------------------------------------
// <copyright file="HttpExceptionHelper.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Activities.Http
{
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;

    using Microsoft.Activities.Http.Activation;
    using Microsoft.ApplicationServer.Http.Dispatcher;

    /// <summary>
    ///   TODO: Update summary.
    /// </summary>
    public static class HttpExceptionHelper
    {
        #region Constants and Fields

        internal static bool IncludeExceptionDetails;

        #endregion

        #region Public Methods

        [Conditional("DEBUG")]
        public static void WriteThread(string format, params object[] args)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("[{0,3}] ", Thread.CurrentThread.ManagedThreadId);
            if (args.Length > 0)
            {
                sb.AppendFormat(format, args);
            }
            else
            {
                sb.Append(format);
            }
            Debug.WriteLine(sb.ToString());
        }

        #endregion

        #region Methods

        internal static HttpResponseException CreateResponseException(HttpStatusCode code, string format, params object[] args)
        {
            var description = (args == null) ? format : string.Format(format, args);
            WriteThread(description);

            if (IncludeExceptionDetails)
            {
                throw new HttpResponseException(new HttpResponseMessage<string>(description, code));
            }

            return new HttpResponseException(code);
        }

        #endregion
    }
}