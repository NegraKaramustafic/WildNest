using System.Text;
using eCommerce.Common.Services;

namespace eCommerce.WebAPI.Configuration;

/// <summary>
/// JWT signing settings bound once from environment variables at startup.
/// </summary>
public sealed class JwtSettings
{
    public string SecretKey { get; }
    public string Issuer { get; }
    public string Audience { get; }
    public int DurationInMinutes { get; }

    private JwtSettings(string secretKey, string issuer, string audience, int durationInMinutes)
    {
        SecretKey = secretKey;
        Issuer = issuer;
        Audience = audience;
        DurationInMinutes = durationInMinutes;
    }

    public static JwtSettings FromEnvironment()
    {
        var secretKey = EnvVariables.Require("JwtToken__SecretKey");
        if (Encoding.UTF8.GetByteCount(secretKey) < 32)
        {
            throw new InvalidOperationException(
                "JwtToken__SecretKey must be at least 32 bytes. Set a longer value in the .env file.");
        }

        var durationText = EnvVariables.Require("JwtToken__DurationInMinutes");
        if (!int.TryParse(durationText, out var durationInMinutes) || durationInMinutes <= 0)
        {
            throw new InvalidOperationException(
                "JwtToken__DurationInMinutes must be a positive integer from the .env file.");
        }

        return new JwtSettings(
            secretKey,
            EnvVariables.Require("JwtToken__Issuer"),
            EnvVariables.Require("JwtToken__Audience"),
            durationInMinutes);
    }
}
