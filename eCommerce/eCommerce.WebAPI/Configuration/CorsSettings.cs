using eCommerce.Common.Services;

namespace eCommerce.WebAPI.Configuration;

/// <summary>
/// Explicit browser origins allowed to call the API. Bound once at startup.
/// </summary>
public sealed class CorsSettings
{
    public string[] AllowedOrigins { get; }

    private CorsSettings(string[] allowedOrigins)
    {
        AllowedOrigins = allowedOrigins;
    }

    public static CorsSettings FromEnvironment()
    {
        var raw = EnvVariables.Require("CORS_ALLOWED_ORIGINS");
        var origins = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (origins.Length == 0)
        {
            throw new InvalidOperationException(
                "CORS_ALLOWED_ORIGINS must list at least one origin in the .env file.");
        }

        return new CorsSettings(origins);
    }
}
