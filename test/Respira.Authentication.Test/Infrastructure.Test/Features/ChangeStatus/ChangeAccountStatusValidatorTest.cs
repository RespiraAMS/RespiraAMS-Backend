using Respira.Authentication.Application.Features.Accounts.Commands.ChangeStatus;
using Respira.Authentication.Domain.Enums;

namespace Respira.Authentication.Infrastructure.Test.Features.ChangeStatus;

public sealed class ChangeAccountStatusValidatorTest
{
    private readonly ChangeAccountStatusValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var command = new ChangeAccountStatusCommand
        {
            TargetAccountId = Guid.NewGuid(),
            Status = StatusType.Inactive,
            ActorAccountId = Guid.NewGuid(),
            ActorRole = "Admin",
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyTargetAccountId_ReturnsError()
    {
        var command = new ChangeAccountStatusCommand
        {
            TargetAccountId = Guid.Empty,
            Status = StatusType.Active,
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("TargetAccountId", error.PropertyName);
    }

    [Fact]
    public void Validate_WithInvalidStatusEnum_ReturnsError()
    {
        var command = new ChangeAccountStatusCommand
        {
            TargetAccountId = Guid.NewGuid(),
            Status = (StatusType)999,
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Status", error.PropertyName);
    }
}
