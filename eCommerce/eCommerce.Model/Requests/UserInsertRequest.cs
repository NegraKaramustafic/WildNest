using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class UserInsertRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ProfileImageBase64 { get; set; }

        /// <summary>
        /// Admin create only. Register ignores this and always assigns Customer.
        /// </summary>
        public string? Role { get; set; }
    }
}
