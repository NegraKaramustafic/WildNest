namespace eCommerce.Model
{
    public static class TicketLimits
    {
        public const int MinimumVisitors = 1;
        public const int MaximumVisitors = 20;
        public const string VisitorCountMessage = "Visitor count must be between 1 and 20.";
        public const string VisitDateRequiredMessage = "VisitDate is required.";
        public const string VisitDatePastMessage = "Visit date cannot be before today.";
    }
}
