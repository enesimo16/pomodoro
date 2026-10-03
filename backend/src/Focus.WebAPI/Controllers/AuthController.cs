using System.Security.Claims;
using Focus.Application.Features.Auth.Commands;
using Focus.Application.Features.Auth.DTOs;
using Focus.Application.Features.Auth.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

public record GuestLoginRequest(string? DisplayName, string? Locale, string? TimeZoneId);
public record GoogleLoginRequest(string IdToken, Guid? GuestUserIdToClaim);
public record RefreshTokenRequest(string? RefreshToken);
public record RevokeTokenRequest(string? RefreshToken);

public class AuthController : BaseApiController
{
    [HttpPost("guest")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Guest([FromBody] GuestLoginRequest? request, CancellationToken cancellationToken)
    {
        var command = new CreateGuestCommand(
            request?.DisplayName,
            request?.Locale,
            request?.TimeZoneId,
            GetClientIpAddress());

        var result = await Mediator.Send(command, cancellationToken);
        SetAuthCookies(result.AccessToken, result.RefreshToken, result.ExpiresAt);

        return Ok(result);
    }

    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Google([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var command = new GoogleLoginCommand(
            request.IdToken,
            request.GuestUserIdToClaim,
            GetClientIpAddress());

        try
        {
            var result = await Mediator.Send(command, cancellationToken);
            SetAuthCookies(result.AccessToken, result.RefreshToken, result.ExpiresAt);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("claim")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Claim([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var command = new GoogleLoginCommand(
            request.IdToken,
            request.GuestUserIdToClaim,
            GetClientIpAddress());

        try
        {
            var result = await Mediator.Send(command, cancellationToken);
            SetAuthCookies(result.AccessToken, result.RefreshToken, result.ExpiresAt);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        var token = request?.RefreshToken ?? Request.Cookies["focus_rt"];
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { message = "Yenileme belirteci bulunamadı." });
        }

        try
        {
            var command = new RefreshTokenCommand(token, GetClientIpAddress());
            var result = await Mediator.Send(command, cancellationToken);
            SetAuthCookies(result.AccessToken, result.RefreshToken, result.ExpiresAt);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            ClearAuthCookies();
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RevokeTokenRequest? request, CancellationToken cancellationToken)
    {
        var token = request?.RefreshToken ?? Request.Cookies["focus_rt"];
        if (!string.IsNullOrEmpty(token))
        {
            var command = new RevokeTokenCommand(token, GetClientIpAddress());
            await Mediator.Send(command, cancellationToken);
        }

        ClearAuthCookies();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await Mediator.Send(new GetCurrentUserQuery(userId), cancellationToken);
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    private void SetAuthCookies(string accessToken, string refreshToken, DateTime accessTokenExpiresAt)
    {
        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = accessTokenExpiresAt
        };

        Response.Cookies.Append("focus_at", accessToken, accessCookieOptions);

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(30)
        };

        Response.Cookies.Append("focus_rt", refreshToken, refreshCookieOptions);
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("focus_at");
        Response.Cookies.Delete("focus_rt");
    }
}
