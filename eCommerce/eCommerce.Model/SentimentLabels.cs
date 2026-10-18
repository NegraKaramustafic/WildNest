namespace eCommerce.Model;

/// <summary>
/// Sentiment classes stored on a review and returned by the API.
/// </summary>
public static class SentimentLabels
{
    public const string Positive = "Positive";
    public const string Negative = "Negative";

    public static readonly IReadOnlyList<string> All = new[] { Positive, Negative };

    public static string? Canonical(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        foreach (var label in All)
        {
            if (string.Equals(label, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return label;
            }
        }

        return null;
    }
}
