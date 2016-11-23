namespace Microsoft.Activities.Http.Activities
{
    using System.Activities.Tracking;

    using Microsoft.Activities.Extensions.Tracking;

    internal class HttpReceiveResponseRecord : CustomTrackingRecord, ICustomTrackingTrace
    {
        #region Constants and Fields

        private const string ResultId = "ResultId";

        #endregion

        #region Constructors and Destructors

        public HttpReceiveResponseRecord(object result)
            : base(typeof(HttpReceiveResponseRecord).Name)
        {
            this.Result = result;
        }

        protected HttpReceiveResponseRecord(HttpReceiveResponseRecord record)
            : base(record)
        {
        }

        #endregion

        #region Properties

        protected object Result
        {
            get
            {
                return this.Data[ResultId];
            }
            set
            {
                this.Data[ResultId] = value;
            }
        }

        #endregion

        #region Implemented Interfaces

        #region ICustomTrackingTrace


        public void Trace(TrackingOptions options = TrackingOptions.Default, System.Diagnostics.TraceSource source = null)
        {
            //throw new System.NotImplementedException();
        }

        public void Trace(TrackingOptions options)
        {
            //System.Diagnostics.Trace.WriteLine(
            //    string.Format(
            //        "{0}: HttpResponse [{1}] \"{2}\" result {3}",
            //        this.RecordNumber,
            //        this.Activity != null ? this.Activity.Id : "null",
            //        this.Activity != null ? this.Activity.Name : "null",
            //        this.Result ?? "null"));
            //TrackingHelper.TraceInstance(options, TrackingOptions.Default, this.InstanceId, this.Annotations, null, null, this.Data, this.EventTime);
        }

        public string ToFormattedString(TrackingOptions options = TrackingOptions.Default)
        {
            throw new System.NotImplementedException();
        }


        #endregion

        #endregion

        #region Methods

        protected override TrackingRecord Clone()
        {
            return new HttpReceiveResponseRecord(this);
        }

        #endregion





    }
}