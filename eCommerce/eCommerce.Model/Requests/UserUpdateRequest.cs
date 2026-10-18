using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class UserUpdateRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public string? ProfileImageBase64 { get; set; }

        /// <summary>
        /// Optional. Applied only when an administrator sends Admin or Customer.
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// Optional. Hashed only when filled in. The current password is not required.
        /// </summary>
        public string? Password { get; set; }

        public string? ConfirmPassword { get; set; }
    }
}
