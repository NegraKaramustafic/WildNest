namespace eCommerce.Model.Requests
{
    public class DonationReportRequest
    {
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public int? ZooPartnerId { get; set; }
    }
}
