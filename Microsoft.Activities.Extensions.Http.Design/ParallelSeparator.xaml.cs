namespace Microsoft.Activities.Http.Design
{
    using System;
    using System.Activities.Presentation;
    using System.Activities.Presentation.Hosting;
    using System.Windows;
    using System.Windows.Media.Animation;

    public partial class ParallelSeparator
    {
        #region Constants and Fields

        public static readonly DependencyProperty AllowedItemTypeProperty =
            DependencyProperty.Register(
                "AllowedItemType",
                typeof(Type),
                typeof(ParallelSeparator),
                new UIPropertyMetadata(typeof(object)));

        public static readonly DependencyProperty ContextProperty = DependencyProperty
            .Register(
                "Context",
                typeof(EditingContext),
                typeof(ParallelSeparator));

        #endregion

        #region Constructors and Destructors

        public ParallelSeparator()
        {
            this.InitializeComponent();
        }

        #endregion

        #region Properties

        public Type AllowedItemType
        {
            get
            {
                return (Type)this.GetValue(AllowedItemTypeProperty);
            }
            set
            {
                this.SetValue(AllowedItemTypeProperty, value);
            }
        }

        public EditingContext Context
        {
            get
            {
                return (EditingContext)this.GetValue(ContextProperty);
            }
            set
            {
                this.SetValue(ContextProperty, value);
            }
        }

        #endregion

        #region Methods

        protected override void OnDragEnter(DragEventArgs e)
        {
            this.CheckAnimate(e, "Expand");
            this.dropTarget.Visibility = Visibility.Visible;
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            this.CheckAnimate(e, "Collapse");
            this.dropTarget.Visibility = Visibility.Collapsed;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            this.dropTarget.Visibility = Visibility.Collapsed;
            base.OnDrop(e);
        }

        private void CheckAnimate(
            DragEventArgs e, string storyboardResourceName)
        {
            if (!e.Handled)
            {
                if (!this.Context.Items.GetValue<ReadOnlyState>().IsReadOnly &&
                    DragDropHelper.AllowDrop(
                        e.Data, this.Context, this.AllowedItemType))
                {
                    this.BeginStoryboard(
                        (Storyboard)this.Resources[storyboardResourceName]);
                    return;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
                e.Handled = true;
            }
        }

        #endregion
    }
}