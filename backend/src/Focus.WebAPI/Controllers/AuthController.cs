using Focus.Application.Features.Auth.Commands;
using Focus.Application.Features.Auth.DTOs;
using Focus.Application.Features.Auth.Queries;
using Focus.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Focus.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Focus.WebAPI.Controllers;

public record GuestLoginRequest(string? DisplayName, string? Locale, string? TimeZoneId);
public record GoogleLoginRequest(string IdToken);
public record RefreshTokenRequest(string? RefreshToken);
public record RevokeTokenRequest(string? RefreshToken);

[Route("api/v1/auth")]
public class AuthController : BaseApiController
{
    private readonly JwtSettings _jwtSettings;

    public AuthController(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    /// <summary>
    /// Sıfır sürtünmeyle anında misafir oturumu açar; varsayılan piksel odayı, avatarı ve tercihleri oluşturarak JWT token döndürür.
    /// </summary>
    [EnableRateLimiting(RateLimitPolicies.Auth)]
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

    /// <summary>
    /// Google OAuth id_token kullanarak giriş yapar veya yeni hesap oluşturur.
    /// </summary>
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Google([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        // Misafir kimligi istemci govdesinden degil, dogrulanmis oturumdan alinir
        var guestUserId = TryGetGuestUserId(out var callerGuestId) ? callerGuestId : (Guid?)null;
        var command = new GoogleLoginCommand(
            request.IdToken,
            guestUserId,
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

    /// <summary>
    /// Mevcut misafir hesabındaki oda, eşya, avatar ve coin ilerlemesini kalıcı bir Google hesabına bağlar (hesap sahiplenme).
    /// </summary>
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("claim")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Claim([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetGuestUserId(out var guestUserId))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Yalnızca misafir hesaplar sahiplenilebilir." });
        }

        var command = new GoogleLoginCommand(
            request.IdToken,
            guestUserId,
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

    /// <summary>
    /// Süresi dolan JWT Access Token'ı geçerli bir Refresh Token ile yeniler.
    /// </summary>
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        var token = string.IsNullOrEmpty(request?.RefreshToken) ? Request.Cookies["focus_rt"] : request.RefreshToken;
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { message = "Oturum geçersiz." });
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

    /// <summary>
    /// Aktif oturumu sonlandırır, Refresh Token'ı iptal eder ve tarayıcı çerezlerini temizler.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RevokeTokenRequest? request, CancellationToken cancellationToken)
    {
        var token = string.IsNullOrEmpty(request?.RefreshToken) ? Request.Cookies["focus_rt"] : request.RefreshToken;
        if (!string.IsNullOrEmpty(token))
        {
            var command = new RevokeTokenCommand(token, GetClientIpAddress());
            await Mediator.Send(command, cancellationToken);
        }

        ClearAuthCookies();
        return NoContent();
    }

    /// <summary>
    /// Giriş yapmış olan mevcut kullanıcının profil, seviye, cüzdan ve temel oda/avatar özetini getirir.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new GetCurrentUserQuery(userId), cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    private bool TryGetGuestUserId(out Guid guestUserId)
    {
        guestUserId = Guid.Empty;
        return User.Identity?.IsAuthenticated == true
            && User.FindFirst("is_guest")?.Value == "true"
            && TryGetUserId(out guestUserId);
    }

    private void SetAuthCookies(string accessToken, string refreshToken, DateTime accessTokenExpiresAt)
    {
        var isDevelopment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment,
            SameSite = SameSiteMode.Lax,
            Expires = accessTokenExpiresAt
        };

        Response.Cookies.Append("focus_at", accessToken, accessCookieOptions);

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
        };

        Response.Cookies.Append("focus_rt", refreshToken, refreshCookieOptions);
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("focus_at");
        Response.Cookies.Delete("focus_rt");
    }
}
