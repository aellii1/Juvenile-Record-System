using System;
using System.Security.Cryptography;
using System.Text;

namespace Juvenile_Record_System.Helpers
{
    public static class PasswordHasher
    {
        // Hash a password using SHA-256
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Verify a password against a stored hash
        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            string hashedEntered = HashPassword(enteredPassword);
            return string.Equals(hashedEntered, storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}