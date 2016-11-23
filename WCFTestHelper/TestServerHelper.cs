// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestServerHelper.cs" company="">
//   
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace WCFTestHelper
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Allows you to start/stop the ASP.NET Web Development Server when unit testing WCF Services
    /// </summary>
    public abstract class TestServerHelper
    {
        #region Properties

        /// <summary>
        /// Gets ServerProcessName.
        /// </summary>
        protected abstract string ServerProcessName { get; }

        /// <summary>
        /// Gets ServerWindowCaption.
        /// </summary>
        protected abstract string ServerWindowCaption { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// The get solution path.
        /// </summary>
        /// <param name="testContextInstance">
        /// The test context instance.
        /// </param>
        /// <returns>
        /// The get solution path.
        /// </returns>
        public static string GetSolutionPath(TestContext testContextInstance)
        {
            return testContextInstance.TestDir.Substring(0, testContextInstance.TestDir.IndexOf("TestResults"));
        }

        /// <summary>
        /// Returns a WebPath assuming that the name of the Web directory is the same
        ///   as the solution name
        /// </summary>
        /// <param name="testContextInstance">
        /// </param>
        /// <param name="webAppPath">
        /// </param>
        /// <returns>
        /// Physical path to the web application
        /// </returns>
        public static string GetWebPathFromSolutionPath(TestContext testContextInstance, string webAppPath = null)
        {
            var solutionPath = GetSolutionPath(testContextInstance);
            var segments = solutionPath.Split('\\');

            if (segments.Length - 2 < 0)
            {
                throw new InvalidOperationException(
                    string.Format("Cannot extract solution name from solution path \"{0}\"", solutionPath));
            }

            var solutionName = segments[segments.Length - 2];
            return solutionPath + (webAppPath ?? solutionName);
        }

        /// <summary>
        /// The close if running.
        /// </summary>
        public virtual void CloseIfRunning()
        {
            var p = Process.GetProcessesByName(this.ServerProcessName);
            if (p.Length == 1)
            {
                Debug.WriteLine(string.Format("Closing {0}", p[0].ProcessName));
                GC.WaitForPendingFinalizers();
                var retry = 3;
                while (retry > 0)
                {
                    p[0].CloseMainWindow();

                    if (p[0].WaitForExit(1000))
                    {
                        break;
                    }

                    retry--;
                    if (retry == 0)
                    {
                        Debug.WriteLine("Unable to close, killing process " + p[0].ProcessName);
                        p[0].Kill();
                        if (!p[0].WaitForExit(1000))
                        {
                            Debug.WriteLine("Unable to kill " + p[0].ProcessName);
                        }
                    }
                }

                p[0].Close();
            }
        }

        /// <summary>
        /// The ensure is running.
        /// </summary>
        /// <param name="serverPath">
        /// The server path.
        /// </param>
        /// <param name="additionalWait">
        /// The additional wait.
        /// </param>
        public virtual void EnsureIsRunning(string serverPath, int additionalWait = 0)
        {
            this.EnsureIsRunning(
                () =>
                    {
                        Debug.WriteLine("Starting {0}", serverPath);
                        Assert.IsNotNull(Process.Start(serverPath));
                        this.WaitForServerWindow();
                        Thread.Sleep(additionalWait);
                    });
        }

        /// <summary>
        /// The ensure is running.
        /// </summary>
        /// <param name="startProcess">
        /// The start process.
        /// </param>
        public virtual void EnsureIsRunning(Action startProcess)
        {
            if (!this.ServerIsRunning())
            {
                startProcess();
                this.WaitForServerWindow();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The server is running.
        /// </summary>
        /// <returns>
        /// true if the server is running.
        /// </returns>
        protected virtual bool ServerIsRunning()
        {
            return this.FindServerWindow() != IntPtr.Zero;
        }

        /// <summary>
        /// The find window.
        /// </summary>
        /// <param name="lpClassName">
        /// The lp class name.
        /// </param>
        /// <param name="lpWindowName">
        /// The lp window name.
        /// </param>
        /// <returns>
        /// The window
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// The find server window.
        /// </summary>
        /// <returns>
        /// The window handle
        /// </returns>
        private IntPtr FindServerWindow()
        {
            var hWnd = FindWindow(null, this.ServerWindowCaption);
            return hWnd;
        }

        /// <summary>
        /// The wait for server window.
        /// </summary>
        /// <param name="retry">
        /// The retry.
        /// </param>
        /// <param name="wait">
        /// The wait.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Timeout while trying to find the window
        /// </exception>
        private void WaitForServerWindow(int retry = 3, int wait = 500)
        {
            var hWnd = this.FindServerWindow();
            while (retry > 0 && hWnd == IntPtr.Zero)
            {
                Thread.Sleep(wait);
                hWnd = this.FindServerWindow();
                retry--;
            }

            if (hWnd == IntPtr.Zero)
            {
                throw new InvalidOperationException("Timeout trying to find window " + this.ServerWindowCaption);
            }
        }

        #endregion
    }
}