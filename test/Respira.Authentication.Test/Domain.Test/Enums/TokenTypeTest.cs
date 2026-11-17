using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Authentication.Domain.Enums;

namespace Respira.Authentication.Domain.Test.Enums
{
    public class TokenTypeTest
    {
        [Fact]
        public void TokenType_HasFourDeclaredMembersWithStableOrdinals()
        {
            var values = Enum.GetValues<TokenType>();

            Assert.Equal(4, values.Length);
            Assert.Equal(0, (int)TokenType.RefreshToken);
            Assert.Equal(1, (int)TokenType.AccessToken);
            Assert.Equal(2, (int)TokenType.EmailVerificationToken);
            Assert.Equal(3, (int)TokenType.PasswordResetToken);
        }

        [Theory]
        [InlineData(TokenType.RefreshToken, "RefreshToken")]
        [InlineData(TokenType.AccessToken, "AccessToken")]
        [InlineData(TokenType.EmailVerificationToken, "EmailVerificationToken")]
        [InlineData(TokenType.PasswordResetToken, "PasswordResetToken")]
        public void ParseAndSerialize_RoundTripEveryMember(TokenType tokenType, string expectedName)
        {
            Assert.Equal(expectedName, tokenType.ToString());
            Assert.Equal(tokenType, Enum.Parse<TokenType>(expectedName));
            Assert.True(Enum.TryParse<TokenType>(expectedName, out var parsed));
            Assert.Equal(tokenType, parsed);
            Assert.True(Enum.IsDefined(tokenType));

            var json = JsonSerializer.Serialize(tokenType);
            Assert.Equal($"\"{expectedName}\"", json);
            Assert.Equal(tokenType, JsonSerializer.Deserialize<TokenType>(json));
        }

        [Fact]
        public void TokenType_IsAnnotatedWithJsonStringEnumConverter()
        {
            var attribute = (JsonConverterAttribute)
                typeof(TokenType)
                    .GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false)
                    .Single();

            Assert.Equal(typeof(JsonStringEnumConverter), attribute.ConverterType);
        }

        [Fact]
        public void IsDefined_RejectsUndefinedOrdinal()
        {
            Assert.False(Enum.IsDefined((TokenType)99));
            Assert.False(Enum.IsDefined((TokenType)(-1)));
        }
    }
}
