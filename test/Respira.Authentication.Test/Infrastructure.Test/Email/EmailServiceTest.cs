using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Respira.Authentication.Application.Contracts.Email;
using Respira.Authentication.Infrastructure.Email;

namespace Respira.Authentication.Infrastructure.Test.Email;

public class EmailServiceTest
{
    private static EmailService CreateService(bool useSsl = false, int port = 2525) =>
        new(
            Options.Create(
                new EmailOption
                {
                    Host = "127.0.0.1",
                    Port = port,
                    UserName = "mailer@respira.ams",
                    Password = "secret",
                    FromEmail = "noreply@respira.ams",
                    FromName = "Respira AMS",
                    UseSsl = useSsl,
                }
            ),
            Mock.Of<ILogger<EmailService>>()
        );

    [Fact]
    public async Task SendAsync_NullRecipientEmail_ThrowsArgumentNullException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendAsync(null!, "Subject", "<p>Body</p>", TestContext.Current.CancellationToken)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_EmptyOrWhitespaceRecipientEmail_ThrowsArgumentException(
        string recipientEmail
    )
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendAsync(
                recipientEmail,
                "Subject",
                "<p>Body</p>",
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task SendAsync_NullSubject_ThrowsArgumentNullException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendAsync(
                "doctor@respira.ams",
                null!,
                "<p>Body</p>",
                TestContext.Current.CancellationToken
            )
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_EmptyOrWhitespaceSubject_ThrowsArgumentException(string subject)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendAsync(
                "doctor@respira.ams",
                subject,
                "<p>Body</p>",
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task SendAsync_NullHtmlBody_ThrowsArgumentNullException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendAsync(
                "doctor@respira.ams",
                "Subject",
                null!,
                TestContext.Current.CancellationToken
            )
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_EmptyOrWhitespaceHtmlBody_ThrowsArgumentException(string htmlBody)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendAsync(
                "doctor@respira.ams",
                "Subject",
                htmlBody,
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task SendAsync_InvalidRecipientEmailFormat_ThrowsParseExceptionBeforeConnecting()
    {
        var service = CreateService();

        await Assert.ThrowsAnyAsync<FormatException>(() =>
            service.SendAsync(
                "not-an-email-address@@@",
                "Verify Email",
                "<p>Hello</p>",
                TestContext.Current.CancellationToken
            )
        );
    }
}
