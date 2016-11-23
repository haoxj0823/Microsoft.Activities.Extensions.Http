namespace HelloHttpWorkflow
{
    using System;
    using System.Activities;
    using System.Activities.Expressions;
    using System.Activities.Statements;
    using System.Activities.XamlIntegration;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Reflection;
    using System.ServiceModel;
    using System.Text;
    using System.Windows.Forms;
    using System.Xaml;
    using System.Xml;

    using Microsoft.Activities.Extensions;
    using Microsoft.Activities.Extensions.Diagnostics;
    using Microsoft.Activities.Http.Activation;
    using Microsoft.Activities.Http.Activities;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.VisualBasic.Activities;

    internal class Program
    {
        #region Constants and Fields

        private static readonly Uri BaseAddress = new Uri("http://localhost:8080");

        #endregion

        #region Methods
        private static Activity CreateRequestReplyService()
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

        [STAThread]
        private static void Main(string[] args)
        {
            var service = CreateRequestReplyService();

            ActivityXamlServicesEx.WriteToFile(service, @"..\..\HttpService.xaml");

            // ActivityXamlServicesEx.WriteToFile(CreateIntResourceService(), @"..\..\TwoReceives.xaml");

            using (var host = new HttpWorkflowServiceHost(service, BaseAddress))
            {
                host.WorkflowExtensions.Add(new TraceTrackingParticipant());
                host.Open();
                Console.WriteLine("Host Listening at {0}- press any key to exit", BaseAddress);
                Console.ReadKey(true);
                host.Close();
            }
        }

        private static Activity CreateIntResourceService()
        {
            var request1 = new DelegateInArgument<HttpRequestMessage>() { Name = "Request1" };
            var response1 = new DelegateOutArgument<object>() { Name = "Response1" };
            var parameters1 = new DelegateInArgument<IDictionary<string, string>> { Name = "Parameters1" };
            var request2 = new DelegateInArgument<HttpRequestMessage>() { Name = "Request2" };
            var response2 = new DelegateOutArgument<object>() { Name = "Response2" };
            var parameters2 = new DelegateInArgument<IDictionary<string, string>> { Name = "Parameters2" };
            var num = new Variable<int>() { Name = "Num" };

            return new Sequence
                    {
                        Variables = { num },
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
                                                                                new Assign
                                                                                    {
                                                                                        DisplayName = "Store arg in variable",
                                                                                        To = new OutArgument<object>(num),
                                                                                        Value =
                                                                                            new InArgument<int>(
                                                                                            // new VisualBasicValue<int>("Request.Content.ReadAs(Of Int32)()"))
                                                                                            new VisualBasicValue<int>("CType(Parameters1(\"Num\"), Int32)"))
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
                                                                                new Assign
                                                                                    {
                                                                                        DisplayName = "Assign response message",
                                                                                        To = new OutArgument<object>(response1),
                                                                                        Value =
                                                                                            new InArgument<int>(num)
                                                                                    }
                                                                            }
                                                                    }
                                                        }
                                            },
                                    }
            };
        }




        #endregion
    }
}