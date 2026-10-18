namespace eCommerce.Model.Requests
{
    public class NewsUpdateRequest
    {
        public string Title { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Kept by the service when no new image is uploaded.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// PNG or JPEG payload. When set, it replaces the current image.
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
