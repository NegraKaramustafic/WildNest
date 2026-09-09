namespace eCommerce.Model.Responses
{
    public class DonationResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public int ZooPartnerId { get; set; }
        public string ZooPartnerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int? PaymentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
