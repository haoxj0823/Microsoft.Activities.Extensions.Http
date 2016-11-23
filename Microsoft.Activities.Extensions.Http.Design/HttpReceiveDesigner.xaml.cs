namespace Microsoft.Activities.Http.Design
{
    using System.Activities.Presentation.Metadata;
    using System.ComponentModel;

    using Microsoft.Activities.Http.Activities;

    // Interaction logic for HttpReceiveDesigner.xaml
    public partial class HttpReceiveDesigner
    {
        #region Constructors and Destructors

        public HttpReceiveDesigner()
        {
            this.InitializeComponent();
        }

        #endregion

        #region Public Methods

        public static void RegisterMetadata(AttributeTableBuilder builder)
        {
            builder.AddCustomAttributes(typeof(HttpReceive), new DesignerAttribute(typeof(HttpReceiveDesigner)));
            builder.AddCustomAttributes(typeof(HttpReceive), new DescriptionAttribute("A receives an HTTP message"));
            builder.AddCustomAttributes(typeof(HttpReceive), typeof(HttpReceive).GetProperty("Body"), BrowsableAttribute.No);
        }

        #endregion
    }
}