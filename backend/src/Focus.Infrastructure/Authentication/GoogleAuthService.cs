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

    public async Task<GoogleUserInfo?> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings();
            if (!string.IsNullOrWhiteSpace(_googleSettings.ClientId))
            {
                settings.Audience = new[] { _googleSettings.ClientId };
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            if (payload == null)
            {
                return null;
            }

            return new GoogleUserInfo(
                payload.Subject,
                payload.Email,
                payload.Name ?? payload.Email,
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
