using Grocery.Core.Helpers;
using Grocery.Core.Models;

namespace TestCore
{
    public class TestHelpers
    {
        [SetUp]
        public void Setup()
        {
        }


        //Happy flow
        [Test]
        public void TestPasswordHelperReturnsTrue()
        {
            string password = "user3";
            string passwordHash = "sxnIcZdYt8wC8MYWcQVQjQ==.FKd5Z/jwxPv3a63lX+uvQ0+P7EuNYZybvkmdhbnkIHA=";
            Assert.IsTrue(PasswordHelper.VerifyPassword(password, passwordHash));
        }

        [TestCase("user1", "IunRhDKa+fWo8+4/Qfj7Pg==.kDxZnUQHCZun6gLIE6d9oeULLRIuRmxmH2QKJv2IM08=")]
        [TestCase("user3", "sxnIcZdYt8wC8MYWcQVQjQ==.FKd5Z/jwxPv3a63lX+uvQ0+P7EuNYZybvkmdhbnkIHA=")]
        public void TestPasswordHelperReturnsTrue(string password, string passwordHash)
        {
            Assert.IsTrue(PasswordHelper.VerifyPassword(password, passwordHash));
        }


        //Unhappy flow
        [Test]
        public void TestPasswordHelperReturnsFalse()
        {
            string password = "user3";
            string passwordHash = "sxnIcZdYt8wC8MYWcQVQjQ";
            Assert.IsFalse(PasswordHelper.VerifyPassword(password, passwordHash));
        }

        [TestCase("user1", "IunRhDKa+fWo8+4/Qfj7Pg")]
        [TestCase("user3", "sxnIcZdYt8wC8MYWcQVQjQ")]
        public void TestPasswordHelperReturnsFalse(string password, string passwordHash)
        {
            Assert.IsFalse(PasswordHelper.VerifyPassword(password, passwordHash));
        }
        [Test]
        public void TestFilter_OnlyInStockProductsRemain()
        {
            var products = new List<Product>
    {
        new Product(1, "Melk", 10, new DateOnly(2025, 10, 15), 1.00m, "Zuivel"),
        new Product(2, "Kaas", 0,  new DateOnly(2025, 12, 15), 5.00m, "Zuivel"),
        new Product(3, "Brood", 5,  new DateOnly(2025, 11,  1), 2.00m, "Bakkerij")
    };

            var filtered = products.Where(p => p.Stock > 0).ToList();

            Assert.That(filtered.All(p => p.Stock > 0), Is.True);

            Assert.That(filtered.Any(p => p.Name == "Kaas"), Is.False);
        }

    }
}