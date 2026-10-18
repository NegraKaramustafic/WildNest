namespace eCommerce.Services.Images
{
    public interface IStoredImageService
    {
        string Save(byte[] content);

        void Delete(string? relativeUrl);

        StoredImageFile? Read(string fileName);
    }

    public sealed class StoredImageFile
    {
        public StoredImageFile(byte[] bytes, string contentType)
        {
            Bytes = bytes;
            ContentType = contentType;
        }

        public byte[] Bytes { get; }

        public string ContentType { get; }
    }
}
