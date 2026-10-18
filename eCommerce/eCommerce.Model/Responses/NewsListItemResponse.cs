namespace eCommerce.Model.Responses
{
    public class NewsListItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public string AuthorName { get; set; } = string.Empty;
    }
}
