using System.Net;
using System.Text.Json;

namespace eCommerce.Services
{
    public static class AuthRateLimitAccount
    {
        public const string Unknown = "unknown";

        public const string KindItemKey = "AuthRateLimitKind";

        public const string AccountItemKey = "AuthRateLimitAccount";

        public static bool TryMatch(string? path, string? method, out AuthRateLimitKind kind)
        {
            kind = default;
            if (!string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var value = (path ?? string.Empty).TrimEnd('/');
            switch (value.ToLowerInvariant())
            {
                case "/access/login":
                    kind = AuthRateLimitKind.Login;
                    return true;
                case "/access/loginwithrefreshtoken":
                    kind = AuthRateLimitKind.Refresh;
                    return true;
                case "/access/forgotpassword":
                    kind = AuthRateLimitKind.ForgotPassword;
                    return true;
                case "/access/resetpassword":
                    kind = AuthRateLimitKind.ResetPassword;
                    return true;
                default:
                    return false;
            }
        }

        public static string ClientIp(IPAddress? address)
        {
            if (address == null)
            {
                return Unknown;
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            return address.ToString();
        }

        public static string FromBody(AuthRateLimitKind kind, string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Unknown;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return Unknown;
                }

                var propertyName = kind switch
                {
                    AuthRateLimitKind.Login => "username",
                    AuthRateLimitKind.Refresh => "refreshToken",
                    _ => "email"
                };

                if (!TryGetString(document.RootElement, propertyName, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    return Unknown;
                }

                var trimmed = value.Trim();
                return kind == AuthRateLimitKind.Refresh
                    ? trimmed
                    : trimmed.ToLowerInvariant();
            }
            catch (JsonException)
            {
                return Unknown;
            }
        }

        private static bool TryGetString(JsonElement element, string name, out string? value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (!property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : null;
                return true;
            }

            value = null;
            return false;
        }
    }
}
