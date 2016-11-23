namespace Microsoft.Activities.Http.Activities
{
    using System;
    using System.Collections.ObjectModel;

    internal class ValidatingCollection<T> : Collection<T>
    {
        #region Properties

        public Action<T> OnAddValidationCallback { get; set; }

        public Action OnMutateValidationCallback { get; set; }

        #endregion

        #region Methods

        internal static void DisallowNullItems(object item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
        }

        protected override void ClearItems()
        {
            this.OnMutate();
            base.ClearItems();
        }

        protected override void InsertItem(int index, T item)
        {
            this.OnAdd(item);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            this.OnMutate();
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, T item)
        {
            this.OnAdd(item);
            this.OnMutate();
            base.SetItem(index, item);
        }

        private void OnAdd(T item)
        {
            if (this.OnAddValidationCallback == null)
            {
                return;
            }
            this.OnAddValidationCallback(item);
        }

        private void OnMutate()
        {
            if (this.OnMutateValidationCallback == null)
            {
                return;
            }
            this.OnMutateValidationCallback();
        }

        #endregion
    }
}