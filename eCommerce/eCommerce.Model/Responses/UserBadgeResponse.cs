namespace eCommerce.Model.Responses
{
    public class UserBadgeResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BadgeId { get; set; }
        public string BadgeName { get; set; } = string.Empty;
        public string? BadgeDescription { get; set; }
        public string? BadgeIconUrl { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public DateTime EarnedAt { get; set; }
    }
}
