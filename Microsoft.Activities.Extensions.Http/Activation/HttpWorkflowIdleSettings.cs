namespace Microsoft.Activities.Http.Activation
{
    using System;

    public class HttpWorkflowIdleSettings
    {
        #region Properties

        public bool PersistOnIdle
        {
            get
            {
                return this.TimeToPersist != TimeSpan.Zero;
            }
        }

        public TimeSpan TimeToPersist { get; set; }

        public TimeSpan TimeToUnload { get; set; }

        public bool UnloadOnIdle
        {
            get
            {
                return this.TimeToUnload != TimeSpan.Zero;
            }
        }

        #endregion
    }
}
