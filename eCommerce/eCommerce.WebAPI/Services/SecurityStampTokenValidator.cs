using System.Security.Claims;
using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.WebAPI.Services.AccessManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.WebAPI.Services;

public static class SecurityStampTokenValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var idValue = principal?.FindFirstValue(ClaimNames.Id);
        var tokenStamp = principal?.FindFirstValue(ClaimNames.SecurityStamp);
        if (!int.TryParse(idValue, out var userId) || string.IsNullOrEmpty(tokenStamp))
        {
            context.Fail("Access token is missing a security stamp.");
            return;
        }

        var cache = context.HttpContext.RequestServices.GetRequiredService<ISecurityStampCache>();
        var currentStamp = await cache.GetOrLoadAsync(userId, async () =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<ECommerceDbContext>();
            return await db.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => user.SecurityStamp)
                .FirstOrDefaultAsync();
        });

        if (!string.Equals(currentStamp, tokenStamp, StringComparison.Ordinal))
        {
            context.Fail("Access token has been revoked.");
        }
    }
}
