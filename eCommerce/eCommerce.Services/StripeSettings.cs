using Stripe;

namespace eCommerce.Services
{
    /// <summary>
    /// Stripe settings sourced only from environment variables (.env / compose).
    /// Values are not read from appsettings and have no hardcoded secrets.
    /// </summary>
    public sealed class StripeSettings
    {
        public string SecretKey { get; }
        public string WebhookSecret { get; }
        public string Currency { get; }
        public string SuccessUrl { get; }
        public string CancelUrl { get; }

        private StripeSettings(
            string secretKey,
            string webhookSecret,
            string currency,
            string successUrl,
            string cancelUrl)
        {
            SecretKey = secretKey;
            WebhookSecret = webhookSecret;
            Currency = currency;
            SuccessUrl = successUrl;
            CancelUrl = cancelUrl;
        }

        public static StripeSettings FromEnvironment()
        {
            var settings = new StripeSettings(
                Require("STRIPE_SECRET_KEY"),
                Require("STRIPE_WEBHOOK_SECRET"),
                Require("STRIPE_CURRENCY").ToLowerInvariant(),
                Require("STRIPE_SUCCESS_URL"),
                Require("STRIPE_CANCEL_URL"));

            StripeConfiguration.ApiKey = settings.SecretKey;
            return settings;
        }

        private static string Require(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"{name} is not set. Add it to the .env file (see .env.example).");
            }

            return value.Trim();
        }
    }
}
