namespace eCommerce.Model.Responses
{
    public class EntryCheckResponse
    {
        public string Kind { get; set; } = "Ticket";

        public TicketResponse? Ticket { get; set; }

        public ReservationCheckResponse? Reservation { get; set; }
    }

    public class ReservationCheckResponse
    {
        public string Code { get; set; } = string.Empty;
        public string VisitorName { get; set; } = string.Empty;
        public string ActivityName { get; set; } = string.Empty;
        public DateOnly ScheduleDate { get; set; }
        public int GuestCount { get; set; }
        public decimal Total { get; set; }
    }
}
