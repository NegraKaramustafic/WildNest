using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class AdoptionResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public int AnimalId { get; set; }
        public string AnimalName { get; set; } = string.Empty;
        public decimal MonthlyAmount { get; set; }
        public string? AnimalImageUrl { get; set; }
        public AdoptionStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public int? PaymentId { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public int? RejectedByUserId { get; set; }
        public string? RejectedByFullName { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }
        public List<string> AllowedActions { get; set; } = new List<string>();
    }
}
