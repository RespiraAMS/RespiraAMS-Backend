using Respira.Patient.Domain.Utils;
using Xunit;

namespace Respira.Patient.Domain.Test.Utils
{
    public class NameNormalizerTest
    {
        [Theory]
        [InlineData("John", "John")]
        [InlineData("john", "John")]
        [InlineData("JOHN", "John")]
        [InlineData("jOHN", "John")]
        [InlineData("JOHN DOE", "John Doe")]
        [InlineData("john doe", "John Doe")]
        [InlineData("JOHN DOE JR", "John Doe Jr")]
        [InlineData("jOhn   dOe         jR", "John Doe Jr")]
        public void NameNormaize_Success(string name, string expected)
        {
            var actual = NameNormalizer.NameNormalize(name);
            Assert.Equal(expected, actual);
        }
    }
}
