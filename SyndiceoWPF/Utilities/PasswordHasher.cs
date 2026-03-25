using BCrypt.Net;

namespace Syndiceo.Utilities
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHashed(string password)
        {
            return !string.IsNullOrEmpty(password) && password.StartsWith("$2") && password.Length == 60;
        }
    }
}