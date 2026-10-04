using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ProjektWPF.Data
{
    /// <summary>
    /// Hashuje hasła algorytmem PBKDF2-HMAC-SHA256 z losową solą.
    /// Format: pbkdf2$iteracje$sól(base64)$hash(base64).
    /// Dla kont założonych przed zmianą algorytmu obsługiwane są też stare hashe SHA-256 (hex) –
    /// po udanym logowaniu należy je podmienić (patrz <see cref="NeedsRehash"/>).
    /// </summary>
    public static class PasswordHasher
    {
        private const string Prefix = "pbkdf2";
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 210_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public static string HashPassword(string password)
        {
            ArgumentNullException.ThrowIfNull(password);

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

            return string.Join('$',
                Prefix,
                Iterations.ToString(CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key));
        }

        public static bool Verify(string password, string storedHash)
        {
            if (password == null || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            return IsLegacyHash(storedHash)
                ? VerifyLegacy(password, storedHash)
                : VerifyPbkdf2(password, storedHash);
        }

        /// <summary>True, gdy hash jest w starym formacie albo użyto mniejszej liczby iteracji niż obecnie.</summary>
        public static bool NeedsRehash(string storedHash)
        {
            if (IsLegacyHash(storedHash))
            {
                return true;
            }

            return TryParse(storedHash, out int iterations, out _, out _) && iterations < Iterations;
        }

        private static bool VerifyPbkdf2(string password, string storedHash)
        {
            if (!TryParse(storedHash, out int iterations, out byte[] salt, out byte[] expected))
            {
                return false;
            }

            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        private static bool TryParse(string storedHash, out int iterations, out byte[] salt, out byte[] key)
        {
            iterations = 0;
            salt = Array.Empty<byte>();
            key = Array.Empty<byte>();

            string[] parts = storedHash.Split('$');
            if (parts.Length != 4 || parts[0] != Prefix ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations) ||
                iterations <= 0)
            {
                return false;
            }

            try
            {
                salt = Convert.FromBase64String(parts[2]);
                key = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            return salt.Length > 0 && key.Length > 0;
        }

        // Stary format: 64 znaki hex (SHA-256 bez soli).
        private static bool IsLegacyHash(string storedHash) =>
            storedHash.Length == 64 && storedHash.All(Uri.IsHexDigit);

        private static bool VerifyLegacy(string password, string storedHash)
        {
            byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            byte[] expected = Convert.FromHexString(storedHash);
            return CryptographicOperations.FixedTimeEquals(digest, expected);
        }
    }
}
