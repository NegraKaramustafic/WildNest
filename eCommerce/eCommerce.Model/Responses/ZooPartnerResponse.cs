namespace eCommerce.Model.Responses
{
    public class ZooPartnerResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal GoalAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public bool IsActive { get; set; }
        public int DonationCount { get; set; }
    }
}
