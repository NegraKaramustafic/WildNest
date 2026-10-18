using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class TicketResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public DateOnly VisitDate { get; set; }
        public int VisitorCount { get; set; }
        public decimal PricePerPerson { get; set; }
        public decimal TotalPrice { get; set; }
        public string Code { get; set; } = string.Empty;
        public string QrCodeData { get; set; } = string.Empty;
        public TicketStatus Status { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public int? VerifiedByUserId { get; set; }
        public string? VerifiedByFullName { get; set; }
        public TicketVerificationMethod? VerificationMethod { get; set; }
        public int? PaymentId { get; set; }
        public bool IsPaid { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Warning { get; set; }
    }
}
