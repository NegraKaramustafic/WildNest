using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace eCommerce.Common.Services.CryptoService
{
    public class CryptoService : ICryptoService
    {
        public string GenerateHash(string password, string salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 10000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(20);
                return Convert.ToBase64String(hash);
            }
        }

        public string GenerateSlat()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        }

        public bool Verify(string hash, string salt, string password)
        {
            var generatedHash = GenerateHash(password, salt);
             return hash == generatedHash;
        }
    }
}
