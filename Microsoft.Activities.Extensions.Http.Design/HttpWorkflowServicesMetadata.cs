namespace Microsoft.Activities.Http.Design
{
    using System.Activities.Presentation.Metadata;

    public sealed class HttpWorkflowServicesMetadata : IRegisterMetadata
    {
        public void Register()
        {
            RegisterAll();
        }

        public static void RegisterAll()
        {
            var builder = new AttributeTableBuilder();
            HttpWorkflowServiceDesigner.RegisterMetadata(builder);
            HttpReceiveDesigner.RegisterMetadata(builder);
            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}