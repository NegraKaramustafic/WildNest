using eCommerce.Common.Services.CryptoService;
using eCommerce.Model.Access;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Responses;
using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.WebAPI.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace eCommerce.WebAPI.Services.AccessManager
{
    public class AccessManager : IAccessManager
    {
        private readonly IUserService _userService;
        private readonly JwtSettings _jwtSettings;
        private readonly ICryptoService _cryptoService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IAuthenticatedUserAccessor _userAccessor;

        public AccessManager(
            IUserService userService,
            JwtSettings jwtSettings,
            ICryptoService cryptoService,
            IRefreshTokenService refreshTokenService,
            IAuthenticatedUserAccessor userAccessor)
        {
            _userService = userService;
            _jwtSettings = jwtSettings;
            _cryptoService = cryptoService;
            _refreshTokenService = refreshTokenService;
            _userAccessor = userAccessor;
        }

        public async Task<UserLoginResponse> LoginAsync(UserLoginRequest request)
        {
            var user = await _userService.GetByUsernameAsync(request.Username);


            if (user == null || !_cryptoService.Verify(user.PasswordHash, user.PasswordSalt, request.Password))
            {
                throw new ClientException("Invalid username or password.");
            }

            if (!user.IsActive)
            {
                throw new ClientException("User is not active");
            }

            var accessToken = await GenerateTokenAsync(user);
            var refreshTokenValue = GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _refreshTokenService.InsertAsync(refreshToken);

            return new UserLoginResponse
            {
                Accesstoken = accessToken,
                Refreshtoken = refreshTokenValue
            };
        }

        public async Task<UserLoginResponse> LoginWithRefreshTokenAsync(RefreshAccessTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                throw new ClientException("Refresh token is required");
            }

            var refreshToken = await _refreshTokenService.GetStoredTokenAsync(request.RefreshToken);

            if (refreshToken == null)
            {
                throw new ClientException("Invalid refresh token");
            }

            if (refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new ClientException("Refresh token has expired");
            }

            var user = await _userService.GetWithRoleByIdAsync(refreshToken.UserId);

            if (user == null)
            {
                throw new ClientException("User not found");
            }

            if (!user.IsActive)
            {
                throw new ClientException("User is not active");
            }

            await _refreshTokenService.DeleteAllUserRefreshTokensAsync(user.Id);

            var accessToken = await GenerateTokenAsync(user);
            var refreshTokenValue = GenerateRefreshToken();

            var token = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _refreshTokenService.InsertAsync(token);

            return new UserLoginResponse
            {
                Accesstoken = accessToken,
                Refreshtoken = refreshTokenValue
            };

        }

        public async Task LogoutAsync()
        {
            var userId = _userAccessor.GetUserId()
                ?? throw new UnauthorizedAccessException("User id claim is missing.");

            await _userService.RevokeSessionsAsync(userId);
        }

        private async Task<string> GenerateTokenAsync(UserResponse user)
        {
            var securityStamp = await _userService.GetSecurityStampAsync(user.Id);
            var secretKey = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimNames.Id, user.Id.ToString()),
                    new Claim(ClaimNames.FirstName, user.FirstName ?? string.Empty),
                    new Claim(ClaimNames.LastName, user.LastName ?? string.Empty),
                    new Claim(ClaimNames.Email, user.Email ?? string.Empty),
                    new Claim(ClaimNames.Role, user.Role ?? "user"),
                    new Claim(ClaimNames.IsActive, user.IsActive.ToString()),
                    new Claim(ClaimNames.SecurityStamp, securityStamp)
                }),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            var randombytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randombytes);
        }

       
    }
}
