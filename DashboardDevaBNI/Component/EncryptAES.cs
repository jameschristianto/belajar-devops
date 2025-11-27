using System.IO;
using System.Security.Cryptography;
using System.Text;
using System;
using DashboardDevaBNI.Component;

namespace DashboardDevaBNI.Component
{
    public class EncryptAES
    {
        public static byte[] DeriveKeyFromString(string input, int length)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                byte[] result = new byte[length];
                Buffer.BlockCopy(hash, 0, result, 0, length);
                return result;
            }
        }

        // Encryption method
        public static string Encrypt(string plainText)
        {
            var Key = GetConfig.AppSetting["AppSettings:Key"];
            var Nonce = GetConfig.AppSetting["AppSettings:Nonce"];
            try
            {
                byte[] keyBytes = DeriveKeyFromString(Key, 32);   // 256-bit key
                byte[] nonceBytes = DeriveKeyFromString(Nonce, 12); // 96-bit nonce

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] cipherBytes = new byte[plainBytes.Length];
                byte[] tag = new byte[16];

                using (var aesGcm = new AesGcm(keyBytes))
                {
                    aesGcm.Encrypt(nonceBytes, plainBytes, cipherBytes, tag);
                }

                // Combine nonce, tag, and cipherBytes for transport
                byte[] combinedBytes = new byte[nonceBytes.Length + tag.Length + cipherBytes.Length];
                Buffer.BlockCopy(nonceBytes, 0, combinedBytes, 0, nonceBytes.Length);
                Buffer.BlockCopy(tag, 0, combinedBytes, nonceBytes.Length, tag.Length);
                Buffer.BlockCopy(cipherBytes, 0, combinedBytes, nonceBytes.Length + tag.Length, cipherBytes.Length);

                return Convert.ToBase64String(combinedBytes);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        // Decryption method
        public static string Decrypt(string encryptedText)
        {
            var Key = GetConfig.AppSetting["AppSettings:Key"];
            var Nonce = GetConfig.AppSetting["AppSettings:Nonce"];

            try
            {
                byte[] keyBytes = DeriveKeyFromString(Key, 32);   // 256-bit key
                byte[] nonceBytes = DeriveKeyFromString(Nonce, 12); // 96-bit nonce

                byte[] combinedBytes = Convert.FromBase64String(encryptedText);

                byte[] tag = new byte[16];   // GCM tag size
                byte[] cipherBytes = new byte[combinedBytes.Length - nonceBytes.Length - tag.Length];

                Buffer.BlockCopy(combinedBytes, nonceBytes.Length, tag, 0, tag.Length);
                Buffer.BlockCopy(combinedBytes, nonceBytes.Length + tag.Length, cipherBytes, 0, cipherBytes.Length);

                byte[] plainBytes = new byte[cipherBytes.Length];

                using (var aesGcm = new AesGcm(keyBytes))
                {
                    aesGcm.Decrypt(nonceBytes, cipherBytes, tag, plainBytes);
                }

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
