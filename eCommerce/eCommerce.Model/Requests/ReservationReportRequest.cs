using eCommerce.Model.Enums;

namespace eCommerce.Model.Requests
{
    public class ReservationReportRequest
    {
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public int? ActivityId { get; set; }
        public ReservationStatus? Status { get; set; }
    }
}
