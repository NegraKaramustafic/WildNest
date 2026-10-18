using System.Buffers.Binary;
using System.IO.Compression;

namespace eCommerce.Services.Images
{
    /// <summary>
    /// Small PNG files for the seeded news rows. Names match the media file rule
    /// so the existing image endpoint can serve them.
    /// </summary>
    public static class NewsSeedImages
    {
        public const string SafariFileName = "00000000000000000000000000a11e01.png";
        public const string PandaFileName = "00000000000000000000000000a11e02.png";
        public const string HabitatFileName = "00000000000000000000000000a11e03.png";
        public const string DraftFileName = "00000000000000000000000000a11e04.png";

        public static readonly string SafariUrl = FileStoredImageService.UrlPrefix + SafariFileName;
        public static readonly string PandaUrl = FileStoredImageService.UrlPrefix + PandaFileName;
        public static readonly string HabitatUrl = FileStoredImageService.UrlPrefix + HabitatFileName;
        public static readonly string DraftUrl = FileStoredImageService.UrlPrefix + DraftFileName;

        public static void EnsurePresent(string directory)
        {
            Directory.CreateDirectory(directory);
            WriteIfMissing(directory, SafariFileName, 62, 122, 76);
            WriteIfMissing(directory, PandaFileName, 196, 92, 38);
            WriteIfMissing(directory, HabitatFileName, 47, 104, 64);
            WriteIfMissing(directory, DraftFileName, 123, 123, 115);
        }

        public static byte[] SolidPng(byte red, byte green, byte blue)
        {
            const int width = 64;
            const int height = 40;
            var raw = new byte[height * (1 + width * 3)];
            var index = 0;
            for (var y = 0; y < height; y++)
            {
                raw[index++] = 0;
                for (var x = 0; x < width; x++)
                {
                    raw[index++] = red;
                    raw[index++] = green;
                    raw[index++] = blue;
                }
            }

            using var compressed = new MemoryStream();
            using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                zlib.Write(raw, 0, raw.Length);
            }

            using var output = new MemoryStream();
            output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var ihdr = new byte[13];
            BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(0), width);
            BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
            ihdr[8] = 8;
            ihdr[9] = 2;
            WriteChunk(output, "IHDR"u8, ihdr);
            WriteChunk(output, "IDAT"u8, compressed.ToArray());
            WriteChunk(output, "IEND"u8, Array.Empty<byte>());
            return output.ToArray();
        }

        private static void WriteIfMissing(string directory, string fileName, byte red, byte green, byte blue)
        {
            var path = Path.Combine(directory, fileName);
            if (File.Exists(path))
            {
                return;
            }

            File.WriteAllBytes(path, SolidPng(red, green, blue));
        }

        private static void WriteChunk(Stream output, ReadOnlySpan<byte> type, byte[] data)
        {
            Span<byte> length = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
            output.Write(length);
            output.Write(type);
            output.Write(data);

            var crcSource = new byte[type.Length + data.Length];
            type.CopyTo(crcSource);
            data.CopyTo(crcSource.AsSpan(type.Length));
            Span<byte> crc = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(crcSource));
            output.Write(crc);
        }

        private static uint Crc32(byte[] data)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var value in data)
            {
                crc ^= value;
                for (var i = 0; i < 8; i++)
                {
                    var mask = (crc & 1) == 1 ? 0xEDB88320u : 0u;
                    crc = (crc >> 1) ^ mask;
                }
            }

            return ~crc;
        }
    }
}
