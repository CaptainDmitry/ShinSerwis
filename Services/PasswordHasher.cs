using System.Security.Cryptography;
using System.Text;

namespace ShinSerwis.Services
{
    public class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 10000;

        public string HashPassword(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = GetHash(password, salt, Iterations, HashSize);

            string hashString = Convert.ToBase64String(hash);
            string saltString = Convert.ToBase64String(salt);

            return $"{hashString}:{saltString}:{Iterations}";
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            string[] parts = hashedPassword.Split(':');
            if (parts.Length != 3)
            {
                return false;
            }

            byte[] hash = Convert.FromBase64String(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            int iterations = int.Parse(parts[2]);

            byte[] testHash = GetHash(providedPassword, salt, iterations, hash.Length);

            return SlowEquals(hash, testHash);
        }

        private byte[] GetHash(string password, byte[] salt, int iterations, int hashSize)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(hashSize);
            }
        }

        private bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }
    }
} 