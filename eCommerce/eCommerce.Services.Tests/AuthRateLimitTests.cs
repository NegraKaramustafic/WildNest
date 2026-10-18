using System.Net;
using eCommerce.Services;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class AuthRateLimitTests
    {
        [Fact]
        public void TryMatch_AcceptsOnlyTheLimitedPostEndpoints()
        {
            Assert.True(AuthRateLimitAccount.TryMatch("/Access/Login", "POST", out var login));
            Assert.Equal(AuthRateLimitKind.Login, login);
            Assert.True(AuthRateLimitAccount.TryMatch("/access/loginwithrefreshtoken/", "POST", out var refresh));
            Assert.Equal(AuthRateLimitKind.Refresh, refresh);
            Assert.True(AuthRateLimitAccount.TryMatch("/Access/ForgotPassword", "POST", out var forgot));
            Assert.Equal(AuthRateLimitKind.ForgotPassword, forgot);
            Assert.True(AuthRateLimitAccount.TryMatch("/Access/ResetPassword", "POST", out var reset));
            Assert.Equal(AuthRateLimitKind.ResetPassword, reset);
            Assert.False(AuthRateLimitAccount.TryMatch("/Access/Register", "POST", out _));
            Assert.False(AuthRateLimitAccount.TryMatch("/Access/Login", "GET", out _));
        }

        [Fact]
        public void FromBody_UsesUsernameEmailOrRefreshToken()
        {
            Assert.Equal("admin1", AuthRateLimitAccount.FromBody(AuthRateLimitKind.Login, """{"username":" Admin1 "}"""));
            Assert.Equal("customer1@gmail.com", AuthRateLimitAccount.FromBody(AuthRateLimitKind.ForgotPassword, """{"Email":"Customer1@gmail.com"}"""));
            Assert.Equal("customer1@gmail.com", AuthRateLimitAccount.FromBody(AuthRateLimitKind.ResetPassword, """{"email":"customer1@gmail.com","code":"123456"}"""));
            Assert.Equal("Token-AbC", AuthRateLimitAccount.FromBody(AuthRateLimitKind.Refresh, """{"refreshToken":"Token-AbC"}"""));
            Assert.Equal(AuthRateLimitAccount.Unknown, AuthRateLimitAccount.FromBody(AuthRateLimitKind.Login, ""));
            Assert.Equal(AuthRateLimitAccount.Unknown, AuthRateLimitAccount.FromBody(AuthRateLimitKind.Login, "{"));
            Assert.Equal(AuthRateLimitAccount.Unknown, AuthRateLimitAccount.FromBody(AuthRateLimitKind.ForgotPassword, """{"username":"admin1"}"""));
        }

        [Fact]
        public void ClientIp_NormalizesMappedAddresses()
        {
            Assert.Equal(AuthRateLimitAccount.Unknown, AuthRateLimitAccount.ClientIp(null));
            Assert.Equal("127.0.0.1", AuthRateLimitAccount.ClientIp(IPAddress.Parse("::ffff:127.0.0.1")));
            Assert.Equal("10.0.0.8", AuthRateLimitAccount.ClientIp(IPAddress.Parse("10.0.0.8")));
        }

        [Fact]
        public void FromEnvironment_RejectsANonPositiveLimit()
        {
            const string name = "AUTH_RATE_LIMIT_LOGIN_PER_ACCOUNT";
            var previous = Environment.GetEnvironmentVariable(name);
            try
            {
                Environment.SetEnvironmentVariable(name, "0");
                var error = Assert.Throws<InvalidOperationException>(AuthRateLimitSettings.FromEnvironment);
                Assert.Contains(name, error.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, previous);
            }
        }

        [Fact]
        public void Defaults_LeaveRoomForAShortDemo()
        {
            var settings = new AuthRateLimitSettings();
            Assert.Equal(60, settings.WindowSeconds);
            Assert.True(settings.LoginPerAccount >= 10);
            Assert.True(settings.LoginPerIp > settings.LoginPerAccount);
            Assert.True(settings.ForgotPasswordPerAccount >= 5);
            Assert.True(settings.ResetPasswordPerAccount >= 10);
            Assert.True(settings.RefreshPerAccount >= 20);
        }
    }
}
