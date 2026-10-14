using eCommerce.Common.Services;
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
                EnvVariables.Require("STRIPE_SECRET_KEY"),
                EnvVariables.Require("STRIPE_WEBHOOK_SECRET"),
                EnvVariables.Require("STRIPE_CURRENCY").ToLowerInvariant(),
                EnvVariables.Require("STRIPE_SUCCESS_URL"),
                EnvVariables.Require("STRIPE_CANCEL_URL"));

            StripeConfiguration.ApiKey = settings.SecretKey;
            return settings;
        }
    }
}
