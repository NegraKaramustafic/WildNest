using DotNetEnv;

namespace eCommerce.Common.Services
{
    /// <summary>
    /// Loads a .env file into process environment variables. Walks up from the
    /// current and base directories so WebAPI and the Subscriber both find
    /// eCommerce/.env whether they run from the project folder or bin/Debug.
    /// </summary>
    public static class EnvFileLoader
    {
        public static void Load()
        {
            foreach (var start in GetStartDirectories())
            {
                var directory = start;
                for (var i = 0; i < 6 && !string.IsNullOrEmpty(directory); i++)
                {
                    var envPath = Path.Combine(directory, ".env");
                    if (File.Exists(envPath))
                    {
                        Env.Load(envPath);
                        return;
                    }

                    directory = Directory.GetParent(directory)?.FullName;
                }
            }
        }

        private static IEnumerable<string> GetStartDirectories()
        {
            yield return Directory.GetCurrentDirectory();
            yield return AppContext.BaseDirectory;
        }
    }
}
