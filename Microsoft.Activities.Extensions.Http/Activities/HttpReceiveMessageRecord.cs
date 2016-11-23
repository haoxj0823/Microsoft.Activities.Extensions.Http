namespace Microsoft.Activities.Http.Activities
{
    using System.Activities.Tracking;

    using Microsoft.Activities.Extensions.Tracking;

    public class HttpReceiveMessageRecord : CustomTrackingRecord, ICustomTrackingTrace
    {
        #region Constants and Fields

        private const string BookmarkNameId = "BookmarkName";

        #endregion

        #region Constructors and Destructors

        public HttpReceiveMessageRecord(string bookmarkName)
            : base(typeof(HttpReceiveMessageRecord).Name)
        {
            this.BookmarkName = bookmarkName;
        }

        protected HttpReceiveMessageRecord(HttpReceiveMessageRecord record)
            : base(record)
        {
        }

        #endregion

        #region Properties

        public string BookmarkName
        {
            get
            {
                return (string)this.Data[BookmarkNameId];
            }
            internal set
            {
                this.Data[BookmarkNameId] = value;
            }
        }

        #endregion

        #region Implemented Interfaces

        #region ICustomTrackingTrace

        public string ToFormattedString(TrackingOptions options = TrackingOptions.Default)
        {
            throw new System.NotImplementedException();
        }

        public void Trace(TrackingOptions options = TrackingOptions.Default, System.Diagnostics.TraceSource source = null)
        {
            //throw new System.NotImplementedException();
        }

        //public void Trace(TrackingOptions options)
        //{
        //    System.Diagnostics.Trace.WriteLine(
        //        string.Format(
        //            "{0}: HttpReceive [{1}] \"{2}\" UriTemplate {3}",
        //            this.RecordNumber,
        //            this.Activity != null ? this.Activity.Id : "null",
        //            this.Activity != null ? this.Activity.Name : "null",
        //            this.BookmarkName));
        //    TrackingHelper.TraceInstance(options, this.InstanceId, this.Annotations, null, null, this.Data, this.EventTime);
        //}

        #endregion

        #endregion

        #region Methods

        protected override TrackingRecord Clone()
        {
            return new HttpReceiveMessageRecord(this);
        }

        #endregion

        
    }
}