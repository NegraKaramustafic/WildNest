namespace eCommerce.Model.Requests
{
    public class NewsInsertRequest
    {
        public string Title { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Set by the service after the uploaded image is stored. Clients send ImageBase64.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// PNG or JPEG payload. Required when adding news.
        /// </summary>
        public string? ImageBase64 { get; set; }

        /// <summary>
        /// image/png or image/jpeg. Required together with ImageBase64.
        /// </summary>
        public string? ImageContentType { get; set; }

        public DateTime PublishedAt { get; set; }

        public bool IsPublished { get; set; } = true;
    }
}
