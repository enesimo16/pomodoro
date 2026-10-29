using Focus.Application.Common.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Focus.Infrastructure.Authentication;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly GoogleAuthSettings _googleSettings;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(
        IOptions<GoogleAuthSettings> googleOptions,
        ILogger<GoogleAuthService> logger)
    {
        _googleSettings = googleOptions.Value;
        _logger = logger;
    }

    public static GoogleJsonWebSignature.ValidationSettings CreateValidationSettings(GoogleAuthSettings googleSettings)
    {
        if (string.IsNullOrWhiteSpace(googleSettings.ClientId))
        {
            throw new UnauthorizedAccessException("Google girişi yapılandırılmamış.");
        }

        return new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { googleSettings.ClientId }
        };
    }

    public async Task<GoogleUserInfo?> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        // Audience olmadan dogrulama yapilmaz; yapilandirma eksikse istek reddedilir
        var settings = CreateValidationSettings(_googleSettings);

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            if (payload == null || string.IsNullOrWhiteSpace(payload.Subject))
            {
                return null;
            }

            return new GoogleUserInfo(
                payload.Subject,
                payload.Email ?? string.Empty,
                payload.EmailVerified,
                payload.Name ?? payload.Email ?? string.Empty,
                payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            LogTokenValidationFailed(_logger, ex);
            return null;
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex);
            return null;
        }
    }

    private static readonly Action<ILogger, Exception?> LogTokenValidationFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1, nameof(ValidateTokenAsync)),
            "Google token validation failed: Invalid JWT");

    private static readonly Action<ILogger, Exception?> LogUnexpectedError =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2, nameof(ValidateTokenAsync)),
            "Unexpected error validating Google token");
}
