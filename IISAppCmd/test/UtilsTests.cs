using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class UtilsTests
    {
        [Test]
        public void Add_ReturnsSumOfTwoIntegers()
        {
            Assert.That(Utils.Add(2, 3), Is.EqualTo(5));
        }
    }
}
