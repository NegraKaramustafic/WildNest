using eCommerce.Model;

namespace eCommerce.Services.Validators
{
    public static class RoleNames
    {
        public static bool IsKnown(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            var value = role.Trim();
            return value.Equals(Roles.Admin, StringComparison.OrdinalIgnoreCase)
                || value.Equals(Roles.Customer, StringComparison.OrdinalIgnoreCase);
        }

        public static string Canonical(string role)
        {
            return role.Trim().Equals(Roles.Admin, StringComparison.OrdinalIgnoreCase)
                ? Roles.Admin
                : Roles.Customer;
        }
    }
}
