using System.Text.RegularExpressions;

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

    /// <summary>
    /// Local and container connection strings keep host, port, and credentials in
    /// ConnectionStrings__DefaultConnection. The catalog always comes from DB_NAME.
    /// </summary>
    public static string DefaultConnection()
    {
        var template = Require("ConnectionStrings__DefaultConnection");
        var dbName = Require("DB_NAME");
        var expanded = Expand(template);

        var withDatabase = Regex.Replace(
            expanded,
            @"(?i)(Database\s*=\s*)([^;]*)",
            match => match.Groups[1].Value + dbName);

        if (!withDatabase.Contains("Database=", StringComparison.OrdinalIgnoreCase))
        {
            withDatabase = withDatabase.Trim().TrimEnd(';') + ";Database=" + dbName;
        }

        return withDatabase;
    }

    private static string Expand(string value)
    {
        return Regex.Replace(value, @"\$\{([A-Za-z_][A-Za-z0-9_]*)\}", match =>
        {
            var name = match.Groups[1].Value;
            var replacement = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(replacement))
            {
                throw new InvalidOperationException(
                    $"{name} is not set. Add it to the .env file (see .env.example).");
            }

            return replacement.Trim();
        });
    }
}
