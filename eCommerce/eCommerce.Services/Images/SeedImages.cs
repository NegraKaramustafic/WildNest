namespace eCommerce.Services.Images
{
    /// <summary>
    /// Stable media names for catalog images committed under Images/Seed.
    /// Startup copies any missing file into the upload directory.
    /// </summary>
    public static class SeedImages
    {
        public const string AnimalKavi = "/media/00000000000000000000000000a11001.png";
        public const string AnimalZara = "/media/00000000000000000000000000a11002.png";
        public const string AnimalMilo = "/media/00000000000000000000000000a11003.png";
        public const string AnimalLuna = "/media/00000000000000000000000000a11004.png";
        public const string AnimalBao = "/media/00000000000000000000000000a11005.png";
        public const string AnimalRio = "/media/00000000000000000000000000a11006.png";
        public const string AnimalNala = "/media/00000000000000000000000000a11007.png";
        public const string AnimalTiko = "/media/00000000000000000000000000a11008.png";

        public const string ActivityElephantFeeding = "/media/00000000000000000000000000ac1001.png";
        public const string ActivityNocturnalSafari = "/media/00000000000000000000000000ac1002.png";
        public const string ActivityBirdWorkshop = "/media/00000000000000000000000000ac1003.png";
        public const string ActivityPandaKeeper = "/media/00000000000000000000000000ac1004.png";
        public const string ActivityRedPandaSnack = "/media/00000000000000000000000000ac1005.png";
        public const string ActivityLemurMeet = "/media/00000000000000000000000000ac1006.png";
        public const string ActivitySnowLeopard = "/media/00000000000000000000000000ac1007.png";
        public const string ActivityLionSavanna = "/media/00000000000000000000000000ac1008.png";
        public const string ActivityMacawWorkshop = "/media/00000000000000000000000000ac1009.png";
        public const string ActivityPandaEnrichment = "/media/00000000000000000000000000ac1010.png";

        public const string PartnerSunbird = "/media/00000000000000000000000000b01001.png";
        public const string PartnerMountain = "/media/00000000000000000000000000b01002.png";
        public const string PartnerCoral = "/media/00000000000000000000000000b01003.png";

        public const string NewsSafari = "/media/00000000000000000000000000a11e01.png";
        public const string NewsPanda = "/media/00000000000000000000000000a11e02.png";
        public const string NewsHabitat = "/media/00000000000000000000000000a11e03.png";
        public const string NewsDraft = "/media/00000000000000000000000000a11e04.png";

        /// <summary>
        /// Copies embedded seed images into the upload directory when they are missing.
        /// Returns how many files were written.
        /// </summary>
        public static int EnsurePresent(string uploadDirectory)
        {
            Directory.CreateDirectory(uploadDirectory);
            var assembly = typeof(SeedImages).Assembly;
            var written = 0;
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                const string marker = ".Seed.";
                var index = resourceName.LastIndexOf(marker, StringComparison.Ordinal);
                if (index < 0 || !resourceName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileName = resourceName[(index + marker.Length)..];
                if (fileName.Length != 36)
                {
                    continue;
                }

                var path = Path.Combine(uploadDirectory, fileName);
                if (File.Exists(path))
                {
                    continue;
                }

                using var source = assembly.GetManifestResourceStream(resourceName);
                if (source == null)
                {
                    continue;
                }

                using var destination = File.Create(path);
                source.CopyTo(destination);
                written++;
            }

            return written;
        }
    }
}
