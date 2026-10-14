namespace eCommerce.Common.Services;

/// <summary>
/// Reads process environment variables populated by <see cref="EnvFileLoader"/>.
/// Callers bind the result once at startup; do not read configuration per request.
/// </summary>
public static class EnvVariables
{
    public static string Require(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{name} is not set. Add it to the .env file (see .env.example).");
        }

        return value.Trim();
    }

    public static string? Optional(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
