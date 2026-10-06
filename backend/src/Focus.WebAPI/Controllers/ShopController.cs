using Focus.Application.Features.Shop.Commands;
using Focus.Application.Features.Shop.DTOs;
using Focus.Application.Features.Shop.Queries;
using Focus.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

public record BuyItemRequest(string CatalogItemId);

public class ShopController : BaseApiController
{
    /// <summary>
    /// Mağazada satılan mobilya, kıyafet, aksesuar ve zemin/duvar kataloğunu kategori, seviye veya arama filtresine göre listeler.
    /// </summary>
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(List<CatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalog(
        [FromQuery] CatalogCategory? category,
        [FromQuery] CatalogTier? tier,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        Guid? userId = null;
        if (TryGetUserId(out var parsedId))
        {
            userId = parsedId;
        }

        var result = await Mediator.Send(new GetCatalogQuery(userId, category, tier, search), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının satın aldığı ve envanterinde bulunan tüm mobilya ve kıyafet eşyalarını getirir.
    /// </summary>
    [Authorize]
    [HttpGet("inventory")]
    [ProducesResponseType(typeof(List<UserInventoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetInventory(
        [FromQuery] CatalogCategory? category,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new GetUserInventoryQuery(userId, category), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Focus Coin harcayarak mağazadan mobilya, kıyafet veya dekoratif bir eşya satın alır.
    /// </summary>
    [Authorize]
    [HttpPost("buy")]
    [ProducesResponseType(typeof(PurchaseItemResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PurchaseItemResultDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BuyItem([FromBody] BuyItemRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new BuyCatalogItemCommand(userId, request.CatalogItemId), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 100 Focus Coin karşılığında seri dondurucu (Streak Freeze) hakkı satın alır (maksimum 2 adet depolanabilir).
    /// </summary>
    [Authorize]
    [HttpPost("buy-freeze")]
    [ProducesResponseType(typeof(BuyFreezeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BuyFreezeResultDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BuyStreakFreeze(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new BuyStreakFreezeCommand(userId), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
