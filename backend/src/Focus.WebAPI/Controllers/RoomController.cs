using System.Security.Claims;
using Focus.Application.Features.Auth.DTOs;
using Focus.Application.Features.Room.Commands;
using Focus.Application.Features.Room.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

public record AddRoomItemRequest(string CatalogItemId, int GridX, int GridY, int Rotation);
public record MoveRoomItemRequest(int GridX, int GridY, int Rotation);
public record UpdateRoomThemeRequest(string? WallpaperCatalogId, string? FloorCatalogId);
public record UpdateRoomDetailsRequest(string Name, bool IsPublic, int MaxVisitors = 5);

public class RoomController : BaseApiController
{
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PixelRoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRoom(CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            return Unauthorized();
        }

        var room = await Mediator.Send(new GetMyRoomQuery(userId), cancellationToken);
        return Ok(room);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PixelRoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomById(Guid id, CancellationToken cancellationToken)
    {
        Guid? userId = null;
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (Guid.TryParse(subClaim, out var parsedId))
        {
            userId = parsedId;
        }

        var room = await Mediator.Send(new GetRoomByIdQuery(id, userId), cancellationToken);
        if (room == null)
        {
            return NotFound();
        }

        return Ok(room);
    }

    [HttpPost("items")]
    [Authorize]
    [ProducesResponseType(typeof(RoomItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddItem([FromBody] AddRoomItemRequest request, CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            return Unauthorized();
        }

        var command = new AddRoomItemCommand(userId, request.CatalogItemId, request.GridX, request.GridY, request.Rotation);
        var item = await Mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetMyRoom), item);
    }

    [HttpPut("items/{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(RoomItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveItem(Guid id, [FromBody] MoveRoomItemRequest request, CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            return Unauthorized();
        }

        var command = new MoveRoomItemCommand(userId, id, request.GridX, request.GridY, request.Rotation);
        var item = await Mediator.Send(command, cancellationToken);
        if (item == null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpDelete("items/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(Guid id, CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            return Unauthorized();
        }

        var command = new RemoveRoomItemCommand(userId, id);
        var removed = await Mediator.Send(command, cancellationToken);
        if (!removed)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPatch("theme")]
    [Authorize]
    [ProducesResponseType(typeof(PixelRoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateTheme([FromBody] UpdateRoomThemeRequest request, CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            return Unauthorized();
        }

        var command = new UpdateRoomThemeCommand(userId, request.WallpaperCatalogId, request.FloorCatalogId);
        var room = await Mediator.Send(command, cancellationToken);

        return Ok(room);
    }

    [HttpPut("details")]
    [Authorize]
    [ProducesResponseType(typeof(PixelRoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateDetails([FromBody] UpdateRoomDetailsRequest request, CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            return Unauthorized();
        }

        var command = new UpdateRoomDetailsCommand(userId, request.Name, request.IsPublic, request.MaxVisitors);
        var room = await Mediator.Send(command, cancellationToken);

        return Ok(room);
    }
}
