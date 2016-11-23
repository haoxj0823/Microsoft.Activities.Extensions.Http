// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace TestDesigner
{
    using System;
    using System.Activities.Presentation;
    using System.Activities.Presentation.Metadata;
    using System.Activities.Presentation.Toolbox;
    using System.Activities.Statements;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;

    using Microsoft.Activities.Http.Activities;
    using Microsoft.Activities.Http.Design;

    using DesignerMetadata = System.Activities.Core.Presentation.DesignerMetadata;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Constants and Fields

        /// <summary>
        ///   The workflow designer.
        /// </summary>
        private WorkflowDesigner workflowDesigner = new WorkflowDesigner();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "MainWindow" /> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            RegisterMetadata();
            this.AddDesigner();
        }

        #endregion

        #region Methods

        /// <summary>
        /// The create toolbox control.
        /// </summary>
        /// <returns>
        /// The toolbox control
        /// </returns>
        private ToolboxControl CreateToolbox()
        {
            var toolboxControl = new ToolboxControl();

            toolboxControl.Categories.Add(
                new ToolboxCategory("Control Flow")
                    {
                        new ToolboxItemWrapper(typeof(DoWhile)),
                        new ToolboxItemWrapper(typeof(ForEach<>)),
                        new ToolboxItemWrapper(typeof(If)),
                        new ToolboxItemWrapper(typeof(Parallel)),
                        new ToolboxItemWrapper(typeof(ParallelForEach<>)),
                        new ToolboxItemWrapper(typeof(Pick)),
                        new ToolboxItemWrapper(typeof(PickBranch)),
                        new ToolboxItemWrapper(typeof(Sequence)),
                        new ToolboxItemWrapper(typeof(Switch<>)),
                        new ToolboxItemWrapper(typeof(While)),
                    });

            toolboxControl.Categories.Add(
                new ToolboxCategory("Primitives")
                    {
                        new ToolboxItemWrapper(typeof(Assign)),
                        new ToolboxItemWrapper(typeof(Delay)),
                        new ToolboxItemWrapper(typeof(InvokeMethod)),
                        new ToolboxItemWrapper(typeof(WriteLine)),
                    });

            toolboxControl.Categories.Add(
                new ToolboxCategory("HTTP")
                    {
                        new ToolboxItemWrapper(typeof(HttpWorkflowService)),
                        new ToolboxItemWrapper(typeof(HttpReceive)),
                        new ToolboxItemWrapper(typeof(HttpWorkflowServiceFactory)),
                        new ToolboxItemWrapper(typeof(HttpReceiveFactory)),
                    });

            return toolboxControl;
        }

        /// <summary>
        /// The register metadata.
        /// </summary>
        private static void RegisterMetadata()
        {
            var metaData = new DesignerMetadata();
            metaData.Register();
            HttpWorkflowServicesMetadata.RegisterAll();
        }

        /// <summary>
        /// The add designer.
        /// </summary>
        private void AddDesigner()
        {
            // Create an instance of WorkflowDesigner class
            this.workflowDesigner = new WorkflowDesigner();

            // Place the WorkflowDesigner in the middle column of the grid
            Grid.SetColumn(this.workflowDesigner.View, 1);

            // Show the XAML when the model changes
            this.workflowDesigner.ModelChanged += this.ShowWorkflowXaml;

            // Load a new Sequence as default.
            this.workflowDesigner.Load(HttpWorkflowServiceFactory.Create());
            //    new HttpWorkflowService()
            //        {
            //            Body = new Sequence()
            //                {
            //                    Activities =
            //                        {
            //                            new HttpReceive()
            //                                {
            //                                    DisplayName = "HttpReceive",
            //                                    CanCreateInstance = true,
            //                                    Method = "GET",
            //                                    UriTemplate = "/{Id}"
            //                                }
            //                        }
            //                }
            //        }
            //    );

            // Add the WorkflowDesigner to the grid
            this.grid1.Children.Add(this.workflowDesigner.View);

            // Add the Property Inspector
            Grid.SetColumn(this.workflowDesigner.PropertyInspectorView, 2);
            this.grid1.Children.Add(this.workflowDesigner.PropertyInspectorView);

            // Add the toolbox
            ToolboxControl tc = CreateToolbox();
            Grid.SetColumn(tc, 0);
            this.grid1.Children.Add(tc);

            // Show the initial XAML
            this.ShowWorkflowXaml(null, null);
        }

        /// <summary>
        /// The show workflow xaml.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The event args.
        /// </param>
        private void ShowWorkflowXaml(object sender, EventArgs e)
        {
            this.workflowDesigner.Flush();
            this.textXAML.Text = this.workflowDesigner.Text;
        }

        #endregion
    }
}