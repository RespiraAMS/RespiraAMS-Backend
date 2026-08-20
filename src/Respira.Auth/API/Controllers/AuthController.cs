using Application.Features.Authentication.Login.Queries;
using Application.Features.Authentication.Login.Result;
using Application.Features.Authentication.Logout;
using Application.Features.Authentication.Refresh.Queries;
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
    /// Authenticates a doctor with email/password and returns JWT access and refresh tokens.
    /// </summary>
    /// <param name="query">Login credentials</param>
    [HttpPost]
    [Route("login")]
    [ProducesResponseType<ApiResponse<LoginResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginQuery query)
    {
        var result = await bus.InvokeAsync<ApiResponse<LoginResult>>(query);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Exchanges a valid refresh token for a fresh access/refresh token pair.
    /// The previous refresh token is revoked (rotated).
    /// </summary>
    /// <param name="query">Refresh request holding the raw refresh token</param>
    [HttpPost]
    [Route("refresh")]
    [ProducesResponseType<ApiResponse<LoginResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<LoginResult>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Refresh([FromBody] RefreshCommand query)
    {
        var result = await bus.InvokeAsync<ApiResponse<LoginResult>>(query);
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
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
    {
        var revoked = await bus.InvokeAsync<bool>(command);
        var message = revoked ? "Logout successful" : "Already logged out";
        return Ok(ApiResponse.Ok(message));
    }
}
