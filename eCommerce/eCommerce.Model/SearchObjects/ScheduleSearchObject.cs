using eCommerce.Model.Enums;

namespace eCommerce.Model.SearchObjects
{
    public class ScheduleSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter schedules by activity id.
        /// </summary>
        public int? ActivityId { get; set; }

        /// <summary>
        /// Filter schedules by a single date.
        /// </summary>
        public DateOnly? Date { get; set; }

        /// <summary>
        /// Inclusive start of a date range filter.
        /// </summary>
        public DateOnly? DateFrom { get; set; }

        /// <summary>
        /// Inclusive end of a date range filter.
        /// </summary>
        public DateOnly? DateTo { get; set; }

        /// <summary>
        /// Filter schedules by status.
        /// </summary>
        public ScheduleStatus? Status { get; set; }
    }
}
