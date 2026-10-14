namespace eCommerce.Model.Responses
{
    public class ActivityTypeResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ActivityCount { get; set; }
    }
}
