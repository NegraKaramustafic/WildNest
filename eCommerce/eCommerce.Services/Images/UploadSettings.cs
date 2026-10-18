namespace eCommerce.Services.Images
{
    public sealed class UploadSettings
    {
        public UploadSettings(string directory)
        {
            Directory = directory;
        }

        public string Directory { get; }
    }
}
