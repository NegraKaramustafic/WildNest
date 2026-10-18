namespace eCommerce.Model.Responses
{
    public class DonationReportDocument
    {
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public string? PartnerName { get; set; }
        public string GeneratedByName { get; set; } = string.Empty;
        public DateTime GeneratedAtUtc { get; set; }
        public int TotalDonations { get; set; }
        public decimal GrandTotal { get; set; }
        public List<DonationPartnerTotal> Partners { get; set; } = new();
        public List<DonationReportLine> Lines { get; set; } = new();
    }

    public class DonationReportLine
    {
        public string PartnerName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class DonationPartnerTotal
    {
        public int ZooPartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int DonationCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
