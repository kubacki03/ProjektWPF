using System.Security.Cryptography;
using System.Text;
using ProjektWPF.Data;

namespace TestyWpf
{
    public class PasswordHasherTests
    {
        private static string LegacyHash(string password) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();

        [Test]
        public void HashPassword_ThenVerify_Succeeds()
        {
            string hash = PasswordHasher.HashPassword("tajne-haslo");

            Assert.That(PasswordHasher.Verify("tajne-haslo", hash), Is.True);
        }

        [Test]
        public void Verify_WrongPassword_Fails()
        {
            string hash = PasswordHasher.HashPassword("tajne-haslo");

            Assert.That(PasswordHasher.Verify("inne-haslo", hash), Is.False);
        }

        [Test]
        public void HashPassword_SamePasswordTwice_ProducesDifferentHashes()
        {
            string first = PasswordHasher.HashPassword("tajne-haslo");
            string second = PasswordHasher.HashPassword("tajne-haslo");

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void HashPassword_DoesNotContainPlainPassword()
        {
            Assert.That(PasswordHasher.HashPassword("tajne-haslo"), Does.Not.Contain("tajne-haslo"));
        }

        [TestCase("")]
        [TestCase("nie-jest-hashem")]
        [TestCase("pbkdf2$abc$def$ghi")]
        [TestCase("pbkdf2$1000$!!!$!!!")]
        public void Verify_MalformedHash_ReturnsFalseInsteadOfThrowing(string stored)
        {
            Assert.That(PasswordHasher.Verify("cokolwiek", stored), Is.False);
        }

        [Test]
        public void Verify_NullPassword_ReturnsFalse()
        {
            string hash = PasswordHasher.HashPassword("x");

            Assert.That(PasswordHasher.Verify(null!, hash), Is.False);
        }

        [Test]
        public void Verify_LegacySha256Hash_StillWorks()
        {
            string legacy = LegacyHash("stare-haslo");

            Assert.Multiple(() =>
            {
                Assert.That(PasswordHasher.Verify("stare-haslo", legacy), Is.True);
                Assert.That(PasswordHasher.Verify("zle-haslo", legacy), Is.False);
            });
        }

        [Test]
        public void NeedsRehash_LegacyHash_IsTrue()
        {
            Assert.That(PasswordHasher.NeedsRehash(LegacyHash("a")), Is.True);
        }

        [Test]
        public void NeedsRehash_FreshHash_IsFalse()
        {
            Assert.That(PasswordHasher.NeedsRehash(PasswordHasher.HashPassword("a")), Is.False);
        }

        [Test]
        public void NeedsRehash_HashWithFewerIterations_IsTrue()
        {
            string weak = $"pbkdf2$1000${Convert.ToBase64String(new byte[16])}${Convert.ToBase64String(new byte[32])}";

            Assert.That(PasswordHasher.NeedsRehash(weak), Is.True);
        }
    }
}
