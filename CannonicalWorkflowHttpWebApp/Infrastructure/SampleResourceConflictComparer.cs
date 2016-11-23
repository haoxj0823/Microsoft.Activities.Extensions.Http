namespace CannonicalWorkflowHttpWebApp.Infrastructure
{
    using System.Collections.Generic;

    using CannonicalWorkflowHttpWebApp.Models;

    public class SampleResourceConflictComparer : IEqualityComparer<KeyValuePair<int, Sample>>
    {
        #region IEqualityComparer<KeyValuePair<int,HttpResource>> Members

        public bool Equals(KeyValuePair<int, Sample> x, KeyValuePair<int, Sample> y)
        {
            return x.Value.Data == y.Value.Data;
        }

        public int GetHashCode(KeyValuePair<int, Sample> obj)
        {
            return obj.Value.Data.GetHashCode();
        }

        #endregion
    }
}