// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpWorkflowServiceTest.cs" company="">
//   
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Activities.Http.Tests
{
    using System;
    using System.Activities;
    using System.Activities.Expressions;
    using System.Activities.Statements;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Net.Http.Headers;

    using Microsoft.Activities.Extensions;
    using Microsoft.Activities.Http.Activation;
    using Microsoft.Activities.Http.Activities;
    using Microsoft.Activities.Http.UnitTesting;
    using Microsoft.Activities.Extensions.Tracking;
    using Microsoft.Activities.UnitTesting;
    using Microsoft.VisualBasic.Activities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests to verify the HttpWorkflowService class
    /// </summary>
    /// <remarks>
    /// TODO Tests
    ///   * Verify that extensions added to the host are passed through to the WorkflowApplication instance
    ///   * Verify that timeouts work
    ///   * Verify that persistence is not allowed during receive
    ///   * Verify persistence / correlation
    /// </remarks>
    [TestClass]
    public class HttpWorkflowServiceTest
    {
        #region Constants and Fields

        /// <summary>
        ///   The json media type.
        /// </summary>
        private const string JsonMediaType = "application/json";

        /// <summary>
        ///   The base address.
        /// </summary>
        //private static readonly Uri BaseAddress = new Uri("http://ipv4.fiddler:8080");

        // Note: Use SetUrlACL 8080 to enable non-admin access to this http port

        private static readonly Uri BaseAddress = new Uri("http://localhost:7070");

        /// <summary>
        ///   The test timeout.
        /// </summary>
        private readonly TimeSpan testTimeout = TimeSpan.FromSeconds(5);

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets TestContext.
        /// </summary>
        public TestContext TestContext { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Verifies that a workflow which is accessed concurrently will cause the second client to wait until the first episode completes
        /// </summary>
        /// <remarks>
        /// Given
        ///   * A Workflow with two HttpReceive activities
        ///   * The first one has CanCreateInstance = true, Method POST
        ///   * A delay will cause the POST to take some time
        ///   * The second one has CanCreateInstance = false Method GET
        ///   When
        ///   * The first Http message is sent 
        ///   Then the workflow is created and the value 1 is stored in the variable Num
        ///   While the first HttpMessasge is still being processed
        ///   * The second Http message is sent, 
        ///   Then the second client blocks until the workflow goes idle and then 
        ///   the workflow returns the value stored in Num (1)
        /// </remarks>
        [TestMethod]
        public void ConcurrentAccessShouldBlockUntilIdle()
        {
            const int Key = 1;
            const int ExpectedBody = 2;

            // Arrange
            var activity = SequenceWithThreeHttpPost();
            AddVBSettings(activity);
            Debug.WriteLine(ActivityXamlServicesEx.WriteToString(activity));
            HttpWorkflowServiceTestHost host = null;
            try
            {
                // Act
                using (host = HttpWorkflowServiceTestHost.Open(activity, BaseAddress))
                {
                    host.ServiceHost.WorkflowTimeout = TimeSpan.FromSeconds(30);

                    var client = new HttpClient();
                    var requestUri = string.Format("{0}{1}", BaseAddress, Key);

                    var response = this.TestPost(ExpectedBody, client, requestUri, "Initial post");

                    // Get the correlation header
                    var instanceId = WorkflowCookieCorrelation.FromResponse(response);
                    Assert.AreNotEqual(Guid.Empty, instanceId, "No instance ID cookie found");

                    // Phase 2 - have two concurrent clients post
                    var post2 = this.CreateTestPost(ExpectedBody, requestUri, "Client POST 2", instanceId);
                    var post3 = this.CreateTestPost(ExpectedBody, requestUri, "Client POST 3", instanceId);

                    HttpExceptionHelper.WriteThread("Client POST 2 {0}", requestUri);
                    var post2Task = client.SendAsync(post2);
                    HttpExceptionHelper.WriteThread("Client POST 3 {0}", requestUri);
                    var post3Task = client.SendAsync(post3);

                    var response2 = post2Task.Result;
                    var response3 = post3Task.Result;
                    response2.EnsureSuccessStatusCode();
                    response3.EnsureSuccessStatusCode();
                    Assert.AreEqual(ExpectedBody, response3.Content.ReadAs<int>());
                }
            }
            finally
            {
                if (host != null)
                {
                    host.Tracking.Trace();
                }
            }
        }

        /// <summary>
        /// Verifies that when you try to host an empty activity you get a validation exception
        /// </summary>
        /// <remarks>
        /// Note: The parameters dictionary contains argument names in UPPERCASE and the keys are case sensitive.
        /// </remarks>
        [TestMethod]
        [DeploymentItem(@"XAML\Empty.xaml")]
        public void EmptyActivityShouldThrowWhenHosted()
        {
            AssertHelper.Throws<ValidationException>(() => HttpWorkflowServiceTestHost.Open("Empty.xaml", BaseAddress));
        }

        /// <summary>
        /// Verifies that the body of an HttpReceive activity can access the arguments bound in the UriTemplate
        /// </summary>
        /// <remarks>
        /// Note: The parameters dictionary contains argument names in UPPERCASE and the keys are case sensitive.
        /// </remarks>
        [TestMethod]
        [DeploymentItem(@"XAML\GetArgs.xaml")]
        public void GetCanEchoRequestUri()
        {
            const string Expected = "Arg1 = Foo, Arg2 = Bar, Q1 = val1, Q2 = val2";

            // Arrange
            HttpWorkflowServiceTestHost host = null;
            try
            {
                // Act
                using (host = HttpWorkflowServiceTestHost.Open("GetArgs.xaml", BaseAddress))
                {
                    var client = new HttpClient();
                    var response = this.SendGet(client, BaseAddress + "/Foo/Bar?q1=val1&q2=val2");
                    var content = response.Content.ReadAs<string>();
                    Assert.AreEqual(Expected, content);
                }
            }
            finally
            {
                if (host != null)
                {
                    host.Tracking.Trace();
                }
            }
        }

        /// <summary>
        /// Verifies that the body of an HttpReceive activity can access the arguments bound in the UriTemplate
        /// </summary>
        /// <remarks>
        /// Note: The parameters dictionary contains argument names in UPPERCASE and the keys are case sensitive.
        /// </remarks>
        [TestMethod]
        public void HttpReceiveCanAccessBoundUriTemplateArguments()
        {
            const string Expected = "Arg1 = Foo, Arg2 = Bar, Q1 = val1, Q2 = val2";

            // Arrange
            var activity = ServiceWhichAccessesArgs();
            Debug.WriteLine(ActivityXamlServicesEx.WriteToString(activity));
            HttpWorkflowServiceTestHost host = null;
            try
            {
                // Act
                using (host = HttpWorkflowServiceTestHost.Open(activity, BaseAddress))
                {
                    var client = new HttpClient();
                    var response = this.SendGet(client, BaseAddress + "/Foo/Bar?q1=val1&q2=val2");
                    var content = response.Content.ReadAs<string>();
                    Assert.AreEqual(Expected, content);
                }
            }
            finally
            {
                if (host != null)
                {
                    host.Tracking.Trace();
                }
            }
        }

        /// <summary>
        /// Verifies that a workflow will create, store a variable, unload, load and return the variable value
        /// </summary>
        /// <remarks>
        /// Given
        ///   * A Workflow with HttpReceiveActivity POST, CanCreateInstance = true 
        ///   * And a body which will store the content in an Int32 variable "Num"
        ///   * Followed by a delay of 2 seconds
        ///   * And a second HttpReceiveActivity GET with a body that will return the variable "Num" in the response
        ///   When
        ///   * The first POST message is sent    
        ///   Then the workflow is created 
        ///   When 
        ///   * The UnloadOnIdle timeout is exceeded
        ///   Then the workflow is unloaded
        ///   When
        ///   * The second GET message is sent
        ///   Then
        ///   * The workflow is loaded
        ///   * The variable value is returned
        /// </remarks>
        [TestMethod]
        public void HttpServiceShouldPersistAndLoad()
        {
            const int Key = 1;
            const int ExpectedBody = 2;

            // Arrange
            var activity = ReceivePostDelayReceiveGet();
            Debug.WriteLine(ActivityXamlServicesEx.WriteToString(activity));
            HttpWorkflowServiceTestHost testHost = null;
            try
            {
                // Act
                using (testHost = HttpWorkflowServiceTestHost.Open(activity, BaseAddress, this.testTimeout))
                {
                    testHost.ServiceHost.IdleSettings = new HttpWorkflowIdleSettings
                        {
                            TimeToUnload = TimeSpan.FromSeconds(1)
                        };
                    var client = new HttpClient();
                    var requestUri = string.Format("{0}{1}", BaseAddress, Key);
                    var responsePost = this.TestPost(ExpectedBody, client, requestUri, "First Post");
                    HttpExceptionHelper.WriteThread("Client POST Completed Status {0}", responsePost.StatusCode);
                    responsePost.EnsureSuccessStatusCode();
                    testHost.WaitForUnload();

                    var requestGet = new HttpRequestMessage(HttpMethod.Get, requestUri);
                    WorkflowCookieCorrelation.AddCookie(
                        requestGet, WorkflowCookieCorrelation.FromResponse(responsePost));

                    HttpExceptionHelper.WriteThread("Client GET {0}", requestUri);
                    var responseGet = client.Send(requestGet);
                    responseGet.EnsureSuccessStatusCode();
                    var content = responseGet.Content.ReadAs<int>();
                    Assert.AreEqual(ExpectedBody, content);
                }
            }
            finally
            {
                if (testHost != null)
                {
                    testHost.Tracking.Trace();
                }
            }
        }

        /// <summary>
        /// Verifies that an HttpReceive should be able to access the body content
        /// </summary>
        /// <remarks>
        /// Given
        ///   * An Activity with one HttpReceive activity with a POST method
        ///   When
        ///   * An HTTP POST message is received with a matching URI template
        ///   Then
        ///   * The value of the body is read by the receive activity 
        ///   * The body value is returned in the response message
        /// </remarks>
        [TestMethod]
        public void PostShouldReceiveBodyContent()
        {
            const int ExpectedKey = 1;
            const int ExpectedBodyValue = 2;

            // Arrange
            var activity = ServiceWhichAccessesContent();
            HttpExceptionHelper.WriteThread(ActivityXamlServicesEx.WriteToString(activity));
            AddVBSettings(activity);
            HttpWorkflowServiceTestHost host = null;
            try
            {
                // Act
                using (host = HttpWorkflowServiceTestHost.Open(activity, BaseAddress))
                {
                    var client = new HttpClient();
                    var requestUri = string.Format("{0}{1}", BaseAddress, ExpectedKey);
                    var request = new HttpRequestMessage<int>(ExpectedBodyValue);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
                    var response = client.Post(requestUri, new ObjectContent<int>(ExpectedBodyValue, JsonMediaType));
                    response.EnsureSuccessStatusCode();

                    // Assert
                    var value = response.Content.ReadAsString();
                    var actual = response.Content.ReadAs<int>();
                    Assert.AreEqual(ExpectedBodyValue, actual, "The body value was not correct");
                }
            }
            finally
            {
                if (host != null)
                {
                    host.Tracking.Trace(TrackingOptions.All);
                }
            }
        }

        /// <summary>
        /// Verifies that a workflow which contains multiple HttpReceive activities in a pick will activate from any of them
        /// </summary>
        /// <remarks>
        /// Given
        ///   * A Workflow which contains a parallel activity which contains two HttpReceive activities
        ///   * The first one has CanCreateInstance = true, Method POST UriTemplate /First
        ///   * The second one has CanCreateInstance = false Method POST /Second
        ///   When
        ///   * A POST message is sent to /First
        ///   Then a new instance is created
        ///   When 
        ///   * A POST message is sent to /Second
        ///   Then a new instance is created
        /// </remarks>
        [TestMethod]
        public void ReceivesInPickShouldActivate()
        {
            const int ExpectedBody = 2;

            // Arrange
            var activity = ServiceWithTwoHttpReceivesInAPick();
            AddVBSettings(activity);
            Debug.WriteLine(ActivityXamlServicesEx.WriteToString(activity));
            HttpWorkflowServiceTestHost host = null;
            try
            {
                // Act
                using (host = HttpWorkflowServiceTestHost.Open(activity, BaseAddress))
                {
                    var client = new HttpClient();
                    var response1 = this.TestPost(ExpectedBody, client, BaseAddress + "/First", "First Request");
                    var response2 = this.TestPost(ExpectedBody, client, BaseAddress + "/Second", "Second Request");

                    // Assert
                    Assert.AreEqual("First Receive Executed", response1.Content.ReadAs<string>());
                    Assert.AreEqual("Second Receive Executed", response2.Content.ReadAs<string>());
                }
            }
            finally
            {
                if (host != null)
                {
                    host.Tracking.Trace();
                }
            }
        }

        /// <summary>
        /// Verifies that a workflow which contains two HttpReceive messages will work
        /// </summary>
        /// <remarks>
        /// Given
        ///   * A Workflow with two HttpReceive activities
        ///   * The first one has CanCreateInstance = true, Method POST
        ///   * The second one has CanCreateInstance = false Method GET
        ///   When
        ///   * The first Http message is sent 
        ///   Then the workflow is created and the value 1 is stored in the variable Num
        ///   When 
        ///   * The second Http message is sent, 
        ///   Then the workflow returns the value stored in Num (1)
        /// </remarks>
        [TestMethod]
        public void TwoHttpReceivesShouldWork()
        {
            const int Key = 1;
            const int ExpectedBody = 2;

            // Arrange
            var activity = ServiceWithTwoHttpReceives();
            AddVBSettings(activity);
            Debug.WriteLine(ActivityXamlServicesEx.WriteToString(activity));
            HttpWorkflowServiceTestHost host = null;
            try
            {
                // Act
                using (host = HttpWorkflowServiceTestHost.Open(activity, BaseAddress))
                {
                    var client = new HttpClient();
                    var requestUri = string.Format("{0}{1}", BaseAddress, Key);
                    HttpExceptionHelper.WriteThread("Client POST {0}", requestUri);
                    var requestPost = new HttpRequestMessage(HttpMethod.Post, requestUri)
                        {
                            Content = new ObjectContent<int>(ExpectedBody, JsonMediaType)
                        };
                    requestPost.Headers.Add("TestName", this.TestContext.TestName);
                    var responsePost = client.Send(requestPost);
                    HttpExceptionHelper.WriteThread("Client POST Completed Status {0}", responsePost.StatusCode);

                    responsePost.EnsureSuccessStatusCode();

                    var requestGet = new HttpRequestMessage(HttpMethod.Get, requestUri);
                    WorkflowCookieCorrelation.AddCookie(
                        requestGet, WorkflowCookieCorrelation.FromResponse(responsePost));

                    HttpExceptionHelper.WriteThread("Client GET {0}", requestUri);
                    var responseGet = client.Send(requestGet);
                    responseGet.EnsureSuccessStatusCode();
                    var content = responseGet.Content.ReadAs<int>();
                    Assert.AreEqual(ExpectedBody, content);
                }
            }
            finally
            {
                if (host != null)
                {
                    host.Tracking.Trace();
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The add vb settings.
        /// </summary>
        /// <param name="service">
        /// The service.
        /// </param>
        internal static void AddVBSettings(Activity service)
        {
            var settings = new VisualBasicSettings();
            settings.ImportReferences.Add(
                new VisualBasicImportReference
                    {
                        Assembly = "Microsoft.ApplicationServer.Http", 
                        Import = "Microsoft.ApplicationServer.Http"
                    });
            settings.ImportReferences.Add(
                new VisualBasicImportReference { Assembly = "Microsoft.Net.Http", Import = "System.Net.Http" });

            // Provides HttpContent extensions
            settings.ImportReferences.Add(
                new VisualBasicImportReference { Assembly = "Microsoft.Net.Http.Formatting", Import = "System.Net.Http" });
            VisualBasic.SetSettings(service, settings);
        }

        /// <summary>
        /// The service which accesses args.
        /// </summary>
        /// <returns>
        /// The activity
        /// </returns>
        internal static Activity ServiceWhichAccessesArgs()
        {
            var request = new DelegateInArgument<HttpRequestMessage>();
            var response = new DelegateOutArgument<object>();
            var parameters = new DelegateInArgument<IDictionary<string, string>> { Name = "parameters" };
            return new Sequence
                {
                    Activities =
                        {
                            new HttpReceive
                                {
                                    Method = "GET", 
                                    UriTemplate = "/{arg1}/{arg2}?q1={q1}&q2={q2}", 
                                    CanCreateInstance = true, 
                                    Body =
                                        new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object>
                                            {
                                                Argument1 = request, 
                                                Argument2 = parameters, 
                                                Result = response, 
                                                Handler =
                                                    new Sequence
                                                        {
                                                            Activities =
                                                                {
                                                                    new Assign
                                                                        {
                                                                            DisplayName = "Assign response content", 
                                                                            To = new OutArgument<object>(response), 
                                                                            Value =
                                                                                new InArgument<string>(
                                                                                new VisualBasicValue<string>(
                                                                                "string.Format(\"Arg1 = {0}, Arg2 = {1}, Q1 = {2}, Q2 = {3}\", parameters(\"ARG1\"), parameters(\"ARG2\"), parameters(\"Q1\"), parameters(\"Q2\"))"))
                                                                        }
                                                                }
                                                        }
                                            }
                                }, 
                        }
                };
        }

        /// <summary>
        /// The service which accesses content.
        /// </summary>
        /// <returns>
        /// The activity
        /// </returns>
        internal static Activity ServiceWhichAccessesContent()
        {
            var request = new DelegateInArgument<HttpRequestMessage> { Name = "Request" };
            var response = new DelegateOutArgument<object> { Name = "Response" };
            var parameters = new DelegateInArgument<IDictionary<string, string>> { Name = "Parameters" };
            var num = new Variable<int> { Name = "Num" };

            return new Sequence
                {
                    Variables =
                        {
                            num
                        }, 
                    Activities =
                        {
                            new HttpReceive
                                {
                                    Method = "POST", 
                                    UriTemplate = "/{Num}", 
                                    CanCreateInstance = true, 
                                    Body =
                                        new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object>
                                            {
                                                Argument1 = request, 
                                                Argument2 = parameters, 
                                                Result = response, 
                                                Handler =
                                                    new Sequence
                                                        {
                                                            Activities =
                                                                {
                                                                    new Assign<object>
                                                                        {
                                                                            DisplayName = "Test if request is null", 
                                                                            To = new OutArgument<object>(response), 
                                                                            Value =
                                                                                new VisualBasicValue<object>(
                                                                                "Request.Content.ReadAs(Of Int32)()")
                                                                        }
                                                                }
                                                        }
                                            }
                                }, 
                        }
                };
        }

        /// <summary>
        /// The receive post delay receive get.
        /// </summary>
        /// <returns>
        /// The activity
        /// </returns>
        private static Activity ReceivePostDelayReceiveGet()
        {
            var request1 = new DelegateInArgument<HttpRequestMessage> { Name = "Request1" };
            var response1 = new DelegateOutArgument<object> { Name = "Response1" };
            var parameters1 = new DelegateInArgument<IDictionary<string, string>> { Name = "Parameters1" };
            var num = new Variable<int> { Name = "Num" };

            var request2 = new DelegateInArgument<HttpRequestMessage> { Name = "Request2" };
            var response2 = new DelegateOutArgument<object> { Name = "Response2" };
            var parameters2 = new DelegateInArgument<IDictionary<string, string>> { Name = "Parameters2" };

            var service = new Sequence
                {
                    Variables =
                        {
                            num
                        }, 
                    Activities =
                        {
                            new HttpReceive
                                {
                                    DisplayName = "Receive POST store Num", 
                                    Method = "POST", 
                                    UriTemplate = "/{Num}", 
                                    CanCreateInstance = true, 
                                    PersistBeforeSend = true, 
                                    Body =
                                        new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object>
                                            {
                                                Argument1 = request1, 
                                                Argument2 = parameters1, 
                                                Result = response1, 
                                                Handler =
                                                    new Sequence
                                                        {
                                                            Activities =
                                                                {
                                                                    new Assign<int>
                                                                        {
                                                                            DisplayName = "Store arg in variable", 
                                                                            To = new OutArgument<int>(num), 
                                                                            Value =
                                                                                new VisualBasicValue<int>(
                                                                                "Request1.Content.ReadAs(Of Int32)()")
                                                                        }, 
                                                                }
                                                        }
                                            }
                                }, 
                            new Delay { Duration = TimeSpan.FromSeconds(2) }, 
                            new HttpReceive
                                {
                                    DisplayName = "Receive GET for Num", 
                                    Method = "GET", 
                                    UriTemplate = "/{Num}", 
                                    CanCreateInstance = false, 
                                    Body =
                                        new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object>
                                            {
                                                Argument1 = request2, 
                                                Argument2 = parameters2, 
                                                Result = response2, 
                                                Handler =
                                                    new Sequence
                                                        {
                                                            Activities =
                                                                {
                                                                    new Assign<object>
                                                                        {
                                                                            DisplayName = "Assign response", 
                                                                            To = new OutArgument<object>(response2), 
                                                                            Value = num
                                                                        }, 
                                                                }
                                                        }
                                            }
                                }, 
                        }
                };

            AddVBSettings(service);
            return service;
        }

        /// <summary>
        /// The sequence with three http post.
        /// </summary>
        /// <returns>
        /// The activity
        /// </returns>
        private static Activity SequenceWithThreeHttpPost()
        {
            var request1 = new DelegateInArgument<HttpRequestMessage> { Name = "Request1" };
            var response1 = new DelegateOutArgument<object> { Name = "Response1" };
            var parameters1 = new DelegateInArgument<IDictionary<string, string>> { Name = "Parameters1" };

            var request2 = new DelegateInArgument<HttpRequestMessage> { Name = "Request2" };
            var response2 = new DelegateOutArgument<object> { Name = "Response2" };
            var parameters2 = new DelegateInArgument<IDictionary<string, string>> { Name = "Parameters2" };

            var request3 = new DelegateInArgument<HttpRequestMessage> { Name = "Request3" };
            var response3 = new DelegateOutArgument<object> { Name = "Response3" };
            var parameters3 = new DelegateInArgument<IDictionary<string, string>> { Name = "Parameters3" };

            var num = new Variable<int> { Name = "Num" };

            var service = new Sequence
                {
                    Variables =
                        {
                            num
                        }, 
                    Activities =
                        {
                            new HttpReceive
                                {
                                    Method = "POST", 
                                    UriTemplate = "/{Num}", 
                                    CanCreateInstance = true, 
                                    Body =
                                        new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object>
                                            {
                                                Argument1 = request1, 
                                                Argument2 = parameters1, 
                                                Result = response1, 
                                                Handler =
                                                    new Sequence
                                                        {
                                                            Activities =
                                                                {
                                                                    new Assign<int>
                                                                        {
                                                                            DisplayName = "Store arg in variable", 
                                                                            To = new OutArgument<int>(num), 
                                                                            Value =
                                                                                new VisualBasicValue<int>(
                                                                                "Request1.Content.ReadAs(Of Int32)()")
                                                                        }, 
                                                                }
                                                        }
                                            }
                                }, 
                            new HttpReceive
                                {
                                    Method = "POST", 
                                    UriTemplate = "/{Num}", 
                                    CanCreateInstance = true, 
                                    Body =
                                        new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object>
                                            {
                                                Argument1 = request2, 
                                                Argument2 = parameters2, 
                                                Result = response2, 
                                                Handler =
                                                    new Sequence
                                                        {
                                                            Activities =
                                                                {
                                                                    new Delay
                                                                        {
                                                                            Duration =
                                                                                new InArgument<TimeSpan>(
                                                                                TimeSpan.FromSeconds(1))
                                                                        }
                                                                }
                                                        }
                                            }
                                }, 
                            new HttpReceive
                                {
                                    Method = "POST", 
                                    UriTemplate = "/{Num}", 
                                    CanCreateInstance = true, 
                                    Body =
                                        new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object>
                                            {
                                                Argument1 = request3, 
                                                Argument2 = parameters3, 
                                                Result = response3, 
                                                Handler =
                                                    new Sequence
                                                        {
                                                            Activities =
                                                                {
                                                                    new Assign<object>
                                                                        {
                                                                            DisplayName = "Store arg in variable", 
                                                                            To = new OutArgument<object>(response3), 
                                                                            Value =
                                                                                new VisualBasicValue<object>(
                                                                                "Request3.Content.ReadAsString()")
                                                                        }, 
                                                                }
                                                        }
                                            }
                                }, 
                        }
                };

            AddVBSettings(service);
            return service;
        }

        /// <summary>
        /// The service with two http receives.
        /// </summary>
        /// <returns>
        /// The activity
        /// </returns>
        private static Activity ServiceWithTwoHttpReceives()
        {
            var request1 = new DelegateInArgument<HttpRequestMessage> { Name = "Request1" };
            var response1 = new DelegateOutArgument<object> { Name = "Response1" };
            var parameters1 = new DelegateInArgument<IDictionary<string, string>> { Name = "Parameters1" };

            var request2 = new DelegateInArgument<HttpRequestMessage> { Name = "Request2" };
            var response2 = new DelegateOutArgument<object> { Name = "Response2" };
            var parameters2 = new DelegateInArgument<IDictionary<string, string>> { Name = "Parameters2" };

            var num = new Variable<int> { Name = "Num" };
            var msg = new Variable<HttpRequestMessage>("Request");

            var service = new Sequence
                {
                    Variables =
                        {
                            num, 
                            msg
                        }, 
                    Activities =
                        {
                            new HttpReceive
                                {
                                    Method = "POST", 
                                    UriTemplate = "/{Num}", 
                                    CanCreateInstance = true, 
                                    Body =
                                        new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object>
                                            {
                                                Argument1 = request1, 
                                                Argument2 = parameters1, 
                                                Result = response1, 
                                                Handler =
                                                    new Sequence
                                                        {
                                                            Activities =
                                                                {
                                                                    new DelegateArgumentValue<HttpRequestMessage>(
                                                                        request1)
                                                                        {
                                                                            Result = msg
                                                                        }, 
                                                                    new Assign<int>
                                                                        {
                                                                            DisplayName = "Store arg in variable", 
                                                                            To = new OutArgument<int>(num), 
                                                                            Value =
                                                                                new VisualBasicValue<int>(
                                                                                "Request1.Content.ReadAs(Of Int32)()")
                                                                        }
                                                                }
                                                        }
                                            }
                                }, 
                            new HttpReceive
                                {
                                    Method = "GET", 
                                    UriTemplate = "/{Num}", 
                                    CanCreateInstance = true, 
                                    Body =
                                        new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object>
                                            {
                                                Argument1 = request2, 
                                                Argument2 = parameters2, 
                                                Result = response2, 
                                                Handler =
                                                    new Sequence
                                                        {
                                                            Activities =
                                                                {
                                                                    new Assign<object>
                                                                        {
                                                                            DisplayName = "Assign response message", 
                                                                            To = new OutArgument<object>(response2), 
                                                                            Value = num
                                                                        }
                                                                }
                                                        }
                                            }
                                }, 
                        }
                };

            AddVBSettings(service);
            return service;
        }

        /// <summary>
        /// The service with two http receives in a pick.
        /// </summary>
        /// <returns>
        /// The activity
        /// </returns>
        private static Activity ServiceWithTwoHttpReceivesInAPick()
        {
            var request1 = new DelegateInArgument<HttpRequestMessage> { Name = "Request1" };
            var response1 = new DelegateOutArgument<object> { Name = "Response1" };
            var parameters1 = new DelegateInArgument<IDictionary<string, string>> { Name = "Parameters1" };

            var request2 = new DelegateInArgument<HttpRequestMessage> { Name = "Request2" };
            var response2 = new DelegateOutArgument<object> { Name = "Response2" };
            var parameters2 = new DelegateInArgument<IDictionary<string, string>> { Name = "Parameters2" };
            var responseStr = new Variable<string>();

            var service = new Sequence
                {
                    Variables =
                        {
                            responseStr
                        }, 
                    Activities =
                        {
                            new Pick
                                {
                                    Branches =
                                        {
                                            new PickBranch
                                                {
                                                    DisplayName = "First Branch", 
                                                    Trigger =
                                                        new HttpReceive
                                                            {
                                                                DisplayName = "First Receive", 
                                                                Method = "POST", 
                                                                UriTemplate = "/First", 
                                                                CanCreateInstance = true, 
                                                                Body =
                                                                    new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object>
                                                                        {
                                                                            Argument1 = request1, 
                                                                            Argument2 = parameters1, 
                                                                            Result = response1, 
                                                                            Handler =
                                                                                new Sequence
                                                                                    {
                                                                                        Activities =
                                                                                            {
                                                                                                new Assign<string>
                                                                                                    {
                                                                                                        To = responseStr, 
                                                                                                        Value =
                                                                                                            "First Receive Executed"
                                                                                                    }, 
                                                                                                new Assign<object>
                                                                                                    {
                                                                                                        To =
                                                                                                            new OutArgument<object>(
                                                                                                            response1), 
                                                                                                        Value =
                                                                                                            responseStr
                                                                                                    }
                                                                                            }
                                                                                    }
                                                                        }
                                                            }, 
                                                }, 
                                            new PickBranch
                                                {
                                                    DisplayName = "Second Branch", 
                                                    Trigger =
                                                        new HttpReceive
                                                            {
                                                                DisplayName = "Second Receive", 
                                                                Method = "POST", 
                                                                UriTemplate = "/Second", 
                                                                CanCreateInstance = true, 
                                                                Body =
                                                                    new ActivityFunc<HttpRequestMessage, IDictionary<string, string>, object>
                                                                        {
                                                                            Argument1 = request2, 
                                                                            Argument2 = parameters2, 
                                                                            Result = response2, 
                                                                            Handler =
                                                                                new Sequence
                                                                                    {
                                                                                        Activities =
                                                                                            {
                                                                                                new Assign<string>
                                                                                                    {
                                                                                                        To = responseStr, 
                                                                                                        Value =
                                                                                                            "Second Receive Executed"
                                                                                                    }, 
                                                                                                new Assign<object>
                                                                                                    {
                                                                                                        To =
                                                                                                            new OutArgument<object>(
                                                                                                            response2), 
                                                                                                        Value =
                                                                                                            responseStr
                                                                                                    }
                                                                                            }
                                                                                    }
                                                                        }
                                                            }, 
                                                }
                                        }
                                }
                        }
                };

            AddVBSettings(service);
            return service;
        }

        /// <summary>
        /// The create test get.
        /// </summary>
        /// <param name="requestUri">
        /// The request uri.
        /// </param>
        /// <param name="testInfo">
        /// The test info.
        /// </param>
        /// <param name="workflowInstance">
        /// The workflow instance.
        /// </param>
        /// <returns>
        /// The activity
        /// </returns>
        private HttpRequestMessage CreateTestGet(
            string requestUri, string testInfo = null, Guid workflowInstance = default(Guid))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            if (workflowInstance != default(Guid))
            {
                WorkflowCookieCorrelation.AddCookie(request, workflowInstance);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
            request.Headers.Add("TestName", this.TestContext.TestName);
            request.Headers.Add("TestInfo", testInfo);

            return request;
        }

        /// <summary>
        /// The create test post.
        /// </summary>
        /// <param name="body">
        /// The body.
        /// </param>
        /// <param name="requestUri">
        /// The request uri.
        /// </param>
        /// <param name="testInfo">
        /// The test info.
        /// </param>
        /// <param name="workflowInstance">
        /// The workflow instance.
        /// </param>
        /// <typeparam name="T">
        /// The type of content
        /// </typeparam>
        /// <returns>
        /// The activity
        /// </returns>
        private HttpRequestMessage CreateTestPost<T>(
            T body, string requestUri, string testInfo = null, Guid workflowInstance = default(Guid))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new ObjectContent<T>(body, JsonMediaType)
                };
            if (workflowInstance != default(Guid))
            {
                WorkflowCookieCorrelation.AddCookie(request, workflowInstance);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
            request.Headers.Add("TestName", this.TestContext.TestName);
            request.Headers.Add("TestInfo", testInfo);

            return request;
        }

        /// <summary>
        /// The send get.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="requestUri">
        /// The request uri.
        /// </param>
        /// <param name="testInfo">
        /// The test info.
        /// </param>
        /// <param name="workflowInstance">
        /// The workflow instance.
        /// </param>
        /// <returns>
        /// The response message
        /// </returns>
        private HttpResponseMessage SendGet(
            HttpClient client, string requestUri, string testInfo = null, Guid workflowInstance = default(Guid))
        {
            HttpExceptionHelper.WriteThread("Client GET {0} - {1}", requestUri, testInfo);
            var request = this.CreateTestGet(requestUri, testInfo, workflowInstance);
            var response = client.Send(request);
            response.EnsureSuccessStatusCode();
            return response;
        }

        /// <summary>
        /// The test post.
        /// </summary>
        /// <param name="body">
        /// The body.
        /// </param>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="requestUri">
        /// The request uri.
        /// </param>
        /// <param name="testInfo">
        /// The test info.
        /// </param>
        /// <param name="workflowInstance">
        /// The workflow instance.
        /// </param>
        /// <typeparam name="T">
        /// The content type
        /// </typeparam>
        /// <returns>
        /// The response message
        /// </returns>
        private HttpResponseMessage TestPost<T>(
            T body, HttpClient client, string requestUri, string testInfo = null, Guid workflowInstance = default(Guid))
        {
            HttpExceptionHelper.WriteThread("Client POST {0} - {1}", requestUri, testInfo);
            var request = this.CreateTestPost(body, requestUri, testInfo, workflowInstance);
            var response = client.Send(request);
            response.EnsureSuccessStatusCode();
            HttpExceptionHelper.WriteThread("Client POST Completed Status {0} - {1}", response.StatusCode, testInfo);
            return response;
        }

        #endregion
    }
}