using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(subClaim, out userId);
    }

    protected string? GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedHeader))
        {
            var ip = forwardedHeader.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
