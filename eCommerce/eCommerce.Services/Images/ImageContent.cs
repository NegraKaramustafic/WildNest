using System.Text.RegularExpressions;
using eCommerce.Model.Exceptions;

namespace eCommerce.Services.Images
{
    public static class ImageContent
    {
        public const int MaxBytes = 512 * 1024;
        public const int MaxBase64Length = 700_000;
        public const string InvalidMessage = "Image must be a PNG or JPEG file of at most 512 KB.";

        private static readonly Regex DataUrlPrefix = new(
            @"^data:[^;]+;base64,",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static byte[] DecodeUpload(string? base64, string? contentType)
        {
            var declared = NormalizeContentType(contentType);
            if (declared == null)
            {
                throw new ClientException(InvalidMessage);
            }

            var bytes = DecodeBytes(base64);
            if (Detect(bytes) != declared)
            {
                throw new ClientException(InvalidMessage);
            }

            return bytes;
        }

        /// <summary>
        /// Stores a profile photo as raw base64. Empty input clears the photo.
        /// </summary>
        public static string? NormalizeProfileImage(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var bytes = DecodeBytes(value);
            if (Detect(bytes) == null)
            {
                throw new ClientException(InvalidMessage);
            }

            return Convert.ToBase64String(bytes);
        }

        public static string ExtensionFor(byte[] bytes)
        {
            return Detect(bytes) == "image/png" ? ".png" : ".jpg";
        }

        private static byte[] DecodeBytes(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ClientException(InvalidMessage);
            }

            var payload = DataUrlPrefix.Replace(value.Trim(), string.Empty);
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(payload);
            }
            catch (FormatException)
            {
                throw new ClientException(InvalidMessage);
            }

            if (bytes.Length == 0 || bytes.Length > MaxBytes)
            {
                throw new ClientException(InvalidMessage);
            }

            return bytes;
        }

        public static string? NormalizeContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return null;
            }

            var normalized = contentType.Trim().ToLowerInvariant();
            if (normalized == "image/jpg")
            {
                normalized = "image/jpeg";
            }

            return normalized is "image/png" or "image/jpeg" ? normalized : null;
        }

        public static string? Detect(byte[] bytes)
        {
            if (bytes.Length >= 8 &&
                bytes[0] == 0x89 &&
                bytes[1] == 0x50 &&
                bytes[2] == 0x4E &&
                bytes[3] == 0x47 &&
                bytes[4] == 0x0D &&
                bytes[5] == 0x0A &&
                bytes[6] == 0x1A &&
                bytes[7] == 0x0A)
            {
                return "image/png";
            }

            if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            {
                return "image/jpeg";
            }

            return null;
        }
    }
}
