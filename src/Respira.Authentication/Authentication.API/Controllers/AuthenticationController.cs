using Asp.Versioning;
using Authentication.Application.Features.Authentication.Commands.Login;
using Authentication.Application.Features.Authentication.Commands.Login.Result;
using Authentication.Application.Features.Authentication.Commands.Logout;
using Authentication.Application.Features.Authentication.Commands.Refresh;
using Authentication.Application.Features.Authentication.Commands.Refresh.Result;
using Authentication.Application.Features.Authentication.Commands.SendVerify;
using Authentication.Application.Features.Authentication.Commands.VerifyEmail;
using Microsoft.AspNetCore.Mvc;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Authentication.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Tags("Authentication")]
public sealed class AuthenticationController(IMessageBus bus) : ControllerBase
{
    /// <summary>Authenticates an account and returns an access and refresh token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(Result<LoginResult?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<LoginResult?>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<LoginResult?>>(command, cancellationToken);
        return result.ToApiResponse();
    }

    /// <summary>Revokes the supplied refresh token.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<bool>>(command, cancellationToken);
        return result.ToApiResponse();
    }

    /// <summary>Rotates a refresh token and returns a replacement token pair.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(Result<RefreshResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RefreshResult>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<RefreshResult>>(command, cancellationToken);
        return result.ToApiResponse();
    }

    /// <summary>Sends an email verification link to an active, unverified account.</summary>
    [HttpPost("email-verification")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendEmailVerification(
        [FromBody] SendVerifyCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<bool>>(command, cancellationToken);
        return result.ToApiResponse();
    }

    /// <summary>Verifies an email address using the token from the confirmation link.</summary>
    [HttpPost("email-verification/confirm")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<bool>>(command, cancellationToken);
        return result.ToApiResponse();
    }
}
