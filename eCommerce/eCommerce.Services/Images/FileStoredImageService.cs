using System.Text.RegularExpressions;

namespace eCommerce.Services.Images
{
    public sealed class FileStoredImageService : IStoredImageService
    {
        public const string UrlPrefix = "/media/";

        private static readonly Regex SafeName = new(
            @"^[0-9a-f]{32}\.(png|jpg)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private readonly string _directory;

        public FileStoredImageService(UploadSettings settings)
        {
            _directory = settings.Directory;
            Directory.CreateDirectory(_directory);
        }

        public string Save(byte[] content)
        {
            var name = Guid.NewGuid().ToString("N") + ImageContent.ExtensionFor(content);
            var path = Path.Combine(_directory, name);
            File.WriteAllBytes(path, content);
            return UrlPrefix + name;
        }

        public void Delete(string? relativeUrl)
        {
            var name = FileNameFromUrl(relativeUrl);
            if (name == null)
            {
                return;
            }

            var path = SafePath(name);
            if (path != null && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public StoredImageFile? Read(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || !SafeName.IsMatch(fileName))
            {
                return null;
            }

            var path = SafePath(fileName);
            if (path == null || !File.Exists(path))
            {
                return null;
            }

            var contentType = fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? "image/png"
                : "image/jpeg";
            return new StoredImageFile(File.ReadAllBytes(path), contentType);
        }

        public static string? FileNameFromUrl(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
            {
                return null;
            }

            var trimmed = relativeUrl.Trim();
            if (!trimmed.StartsWith(UrlPrefix, StringComparison.Ordinal))
            {
                return null;
            }

            var name = trimmed[UrlPrefix.Length..];
            return SafeName.IsMatch(name) ? name : null;
        }

        private string? SafePath(string fileName)
        {
            var root = Path.GetFullPath(_directory);
            var path = Path.GetFullPath(Path.Combine(root, fileName));
            var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return path;
        }
    }
}
