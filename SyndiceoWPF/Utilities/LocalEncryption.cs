using System;
using System.Security.Cryptography;
using System.Text;

namespace Syndiceo.Utilities
{
    public static class LocalEncryption
    {
        private static readonly byte[] s_additionalEntropy = Encoding.UTF8.GetBytes("SyndiceoSecretSalt777");

        public static string Protect(string plainText)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedData = ProtectedData.Protect(data, s_additionalEntropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedData);
            }
            catch { return null; }
        }

        public static string Unprotect(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return "";
            try
            {
                byte[] data = Convert.FromBase64String(encryptedText);
                byte[] decryptedData = ProtectedData.Unprotect(data, s_additionalEntropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch { return ""; }
        }
    }
}