using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Services.Images;

namespace eCommerce.Services.Images
{
    public static class ImageUpload
    {
        public static string? StoreIfPresent(IStoredImageService images, string? imageBase64, string? contentType)
        {
            if (string.IsNullOrWhiteSpace(imageBase64))
            {
                return null;
            }

            return images.Save(ImageContent.DecodeUpload(imageBase64, contentType));
        }

        public static string? ResolveUrl(string? existingUrl, string? requestedUrl, string? storedUrl)
        {
            if (storedUrl != null)
            {
                return storedUrl;
            }

            if (requestedUrl != null && string.IsNullOrWhiteSpace(requestedUrl))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(requestedUrl))
            {
                return string.IsNullOrWhiteSpace(existingUrl) ? null : existingUrl;
            }

            return requestedUrl.Trim();
        }

        /// <summary>
        /// Uses a newly stored file when one was uploaded.
        /// An empty requested URL clears the image.
        /// Any other client-supplied path is ignored, so the existing server path stays.
        /// </summary>
        public static string? ResolveUploaded(string? existingUrl, string? requestedUrl, string? storedUrl)
        {
            if (!string.IsNullOrEmpty(storedUrl))
            {
                return storedUrl;
            }

            if (requestedUrl != null && string.IsNullOrWhiteSpace(requestedUrl))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(existingUrl) ? null : existingUrl.Trim();
        }
    }
}
