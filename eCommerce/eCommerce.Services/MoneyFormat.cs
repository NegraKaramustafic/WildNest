using System.Globalization;

namespace eCommerce.Services
{
    /// <summary>
    /// Display-only euro amounts. Stored values are not converted.
    /// Example: €1,250.50.
    /// </summary>
    public static class MoneyFormat
    {
        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

        public static string Format(decimal amount)
        {
            var formatted = Math.Abs(amount).ToString("N2", English);
            return amount < 0 ? $"-€{formatted}" : $"€{formatted}";
        }
    }
}
