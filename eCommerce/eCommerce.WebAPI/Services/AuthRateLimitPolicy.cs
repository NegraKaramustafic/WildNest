using System.Threading.RateLimiting;
using eCommerce.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace eCommerce.WebAPI.Services;

/// <summary>
/// Applies a per-IP window and a per-account window together.
/// Either limit returns 429.
/// </summary>
public sealed class AuthRateLimitPolicy : IRateLimiterPolicy<string>
{
    public const string PolicyName = "auth";

    public const string TooManyAttemptsMessage = "Too many attempts. Try again later.";

    private static readonly AsyncLocal<HttpContext?> Ambient = new();

    private readonly AuthRateLimitSettings _settings;
    private readonly PartitionedRateLimiter<HttpContext> _partitions;
    private readonly ForwardingRateLimiter _forwarder;

    public AuthRateLimitPolicy(AuthRateLimitSettings settings)
    {
        _settings = settings;
        _partitions = PartitionedRateLimiter.CreateChained(
            PartitionedRateLimiter.Create<HttpContext, string>(PartitionByIp),
            PartitionedRateLimiter.Create<HttpContext, string>(PartitionByAccount));
        _forwarder = new ForwardingRateLimiter(_partitions);
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        Ambient.Value = httpContext;
        return RateLimitPartition.Get(PolicyName, _ => _forwarder);
    }

    private RateLimitPartition<string> PartitionByIp(HttpContext httpContext)
    {
        var kind = KindOf(httpContext);
        var limits = _settings.LimitsFor(kind);
        var ip = AuthRateLimitAccount.ClientIp(httpContext.Connection.RemoteIpAddress);
        return RateLimitPartition.GetFixedWindowLimiter(
            $"ip:{kind}:{ip}",
            _ => Window(limits.PerIp));
    }

    private RateLimitPartition<string> PartitionByAccount(HttpContext httpContext)
    {
        var kind = KindOf(httpContext);
        var limits = _settings.LimitsFor(kind);
        var account = httpContext.Items[AuthRateLimitAccount.AccountItemKey] as string;
        if (string.IsNullOrWhiteSpace(account))
        {
            account = AuthRateLimitAccount.Unknown;
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            $"account:{kind}:{account}",
            _ => Window(limits.PerAccount));
    }

    private FixedWindowRateLimiterOptions Window(int permitLimit)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(_settings.WindowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true
        };
    }

    private static AuthRateLimitKind KindOf(HttpContext httpContext)
    {
        if (httpContext.Items[AuthRateLimitAccount.KindItemKey] is AuthRateLimitKind kind)
        {
            return kind;
        }

        if (AuthRateLimitAccount.TryMatch(httpContext.Request.Path.Value, httpContext.Request.Method, out var matched))
        {
            return matched;
        }

        return AuthRateLimitKind.Login;
    }

    private sealed class ForwardingRateLimiter : RateLimiter
    {
        private readonly PartitionedRateLimiter<HttpContext> _inner;

        public ForwardingRateLimiter(PartitionedRateLimiter<HttpContext> inner)
        {
            _inner = inner;
        }

        public override TimeSpan? IdleDuration => null;

        public override RateLimiterStatistics? GetStatistics() => null;

        protected override RateLimitLease AttemptAcquireCore(int permitCount)
        {
            var httpContext = Ambient.Value
                ?? throw new InvalidOperationException("Auth rate limit has no request.");
            return _inner.AttemptAcquire(httpContext, permitCount);
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
        {
            var httpContext = Ambient.Value
                ?? throw new InvalidOperationException("Auth rate limit has no request.");
            return _inner.AcquireAsync(httpContext, permitCount, cancellationToken);
        }
    }
}
