using eCommerce.Common.Services;

namespace eCommerce.Services
{
    public sealed class ReminderSettings
    {
        public int HoursBefore { get; init; } = 24;

        public int CheckIntervalMinutes { get; init; } = 15;

        public static ReminderSettings FromEnvironment()
        {
            return new ReminderSettings
            {
                HoursBefore = ReadPositive("REMINDER_HOURS_BEFORE", 24),
                CheckIntervalMinutes = ReadPositive("REMINDER_CHECK_INTERVAL_MINUTES", 15)
            };
        }

        private static int ReadPositive(string name, int fallback)
        {
            var text = EnvVariables.Optional(name);
            if (text == null)
            {
                return fallback;
            }

            if (!int.TryParse(text, out var value) || value <= 0)
            {
                throw new InvalidOperationException($"{name} must be a positive integer.");
            }

            return value;
        }
    }
}
