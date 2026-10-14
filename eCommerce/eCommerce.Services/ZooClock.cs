using eCommerce.Common.Services;

namespace eCommerce.Services;

/// <summary>
/// Zoo-local calendar clock. The time zone id is read once from ZOO_TIME_ZONE.
/// </summary>
public sealed class ZooClock
{
    private readonly TimeZoneInfo _zone;

    public string TimeZoneId { get; }

    public ZooClock(string timeZoneId)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone))
        {
            throw new InvalidOperationException(
                $"ZOO_TIME_ZONE '{timeZoneId}' is not a valid time zone id. Use an IANA id such as Europe/Sarajevo.");
        }

        TimeZoneId = timeZoneId;
        _zone = zone;
    }

    public static ZooClock FromEnvironment() => new(EnvVariables.Require("ZOO_TIME_ZONE"));

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zone));
}
