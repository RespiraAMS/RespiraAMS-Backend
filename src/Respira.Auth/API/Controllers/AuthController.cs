using Application.Features.Authentication.Login.Queries;
using Application.Features.Authentication.Login.Result;
using Application.Features.Authentication.Logout;
using Application.Features.Authentication.Refresh.Queries;
using Application.Features.Authentication.SendEmailVerification;
using Application.Features.Authentication.VerifyEmail;
using Application.Features.Authentication.GetUser.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.ServiceDefaults.Dtos;
using Wolverine;

namespace Respira.Auth.API.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public class AuthController(IMessageBus bus) : ControllerBase
{
    /// <summary>
    /// Retrieves authenticated doctor information by ID. Called by other services via Wolverine messaging.
    /// </summary>
    /// <param name="id">The doctor identifier.</param>
    [HttpGet]
    [Route("doctors/{id}")]
    [ProducesResponseType<Result<GetAuthDoctorResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<GetAuthDoctorResult>>> GetDoctor(Guid id)
    {
        var query = new GetUserQuery { Id = id };
        var result = await bus.InvokeAsync<Result<GetAuthDoctorResult>>(query);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Authenticates a doctor with email/password and returns JWT access and refresh tokens.
    /// </summary>
    /// <param name="query">Login credentials</param>
    [HttpPost]
    [Route("login")]
    [ProducesResponseType<Result<LoginResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginQuery query)
    {
        var result = await bus.InvokeAsync<Result<LoginResult>>(query);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Exchanges a valid refresh token for a fresh access/refresh token pair.
    /// The previous refresh token is revoked (rotated).
    /// </summary>
    /// <param name="query">Refresh request holding the raw refresh token</param>
    [HttpPost]
    [Route("refresh")]
    [ProducesResponseType<Result<LoginResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result<LoginResult>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Refresh([FromBody] RefreshCommand query)
    {
        var result = await bus.InvokeAsync<Result<LoginResult>>(query);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Logs the current session out by revoking the access and refresh tokens
    /// (added to the blacklist) and removing the persisted refresh token.
    /// Returns 200 even when the tokens were already absent (idempotent logout).
    /// </summary>
    /// <param name="command">Access and refresh tokens to revoke</param>
    [HttpPost]
    [Route("logout")]
    [ProducesResponseType<Result>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
    {
        var revoked = await bus.InvokeAsync<bool>(command);
        var message = revoked ? "Logout successful" : "Already logged out";
        return Ok(Result.Ok(message));
    }

    /// <summary>
    /// Confirms a doctor's email using the verification token from the email link.
    /// Called as a GET from the verification link (no body, token/email in query).
    /// </summary>
    /// <param name="command">Verification token and email from the query string</param>
    [HttpGet]
    [Route("verify-email")]
    [ProducesResponseType<Result<bool>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyEmail([FromQuery] VerifyEmailCommand command)
    {
        var result = await bus.InvokeAsync<Result<bool>>(command);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// (Re)sends the email verification link for a registered doctor account.
    /// Generates and persists a fresh verification token, then emails the link.
    /// </summary>
    /// <param name="command">Email of the account to verify</param>
    [HttpPost]
    [Route("resend-verification-email")]
    [ProducesResponseType<Result<bool>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResendVerificationEmail(
        [FromBody] RequestEmailVerificationCommand command
    )
    {
        var result = await bus.InvokeAsync<Result<bool>>(command);
        return StatusCode(result.StatusCode, result);
    }
}
