using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Shop.DTOs;
using Focus.Domain.Enums;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Shop;

public class ShopApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ShopApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCatalog_ShouldReturnAllCatalogItems()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("KatalogGezgini");

        var response = await client.GetAsync("/api/v1/shop/catalog");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalog = await response.Content.ReadFromJsonAsync<List<CatalogItemDto>>();
        catalog.Should().NotBeNull();
        catalog!.Count.Should().BeGreaterThanOrEqualTo(20);
    }

    [Fact]
    public async Task GetCatalog_WithCategoryFilter_ShouldReturnOnlyFilteredItems()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("FiltreGezgini");

        var response = await client.GetAsync($"/api/v1/shop/catalog?category={CatalogCategory.Desk}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalog = await response.Content.ReadFromJsonAsync<List<CatalogItemDto>>();
        catalog.Should().NotBeNull();
        catalog!.Should().OnlyContain(i => i.Category == CatalogCategory.Desk);
    }

    [Fact]
    public async Task BuyItem_WhenFreeItem_ShouldSucceedAndAppearInInventory()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("UcretsizAlan");

        // desk_retro_oak is free (0 coins) and level 1
        var buyResponse = await client.PostAsJsonAsync("/api/v1/shop/buy", new
        {
            catalogItemId = "desk_retro_oak"
        });

        buyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var buyResult = await buyResponse.Content.ReadFromJsonAsync<PurchaseItemResultDto>();
        buyResult.Should().NotBeNull();
        buyResult!.Success.Should().BeTrue();
        buyResult.InventoryItem.Should().NotBeNull();
        buyResult.InventoryItem!.CatalogItemId.Should().Be("desk_retro_oak");

        // Envanter kontrolu
        var invResponse = await client.GetAsync("/api/v1/shop/inventory");
        invResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var inventory = await invResponse.Content.ReadFromJsonAsync<List<UserInventoryItemDto>>();
        inventory.Should().NotBeNull();
        inventory!.Should().Contain(i => i.CatalogItemId == "desk_retro_oak");
    }

    [Fact]
    public async Task BuyItem_WhenInsufficientCoins_ShouldReturnBadRequest()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("FakirAlici");

        // desk_cyber_neon costs 250 coins and level 8
        var buyResponse = await client.PostAsJsonAsync("/api/v1/shop/buy", new
        {
            catalogItemId = "desk_cyber_neon"
        });

        buyResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var buyResult = await buyResponse.Content.ReadFromJsonAsync<PurchaseItemResultDto>();
        buyResult.Should().NotBeNull();
        buyResult!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task BuyStreakFreeze_WhenInsufficientCoins_ShouldReturnBadRequest()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("DondurucuAlicisi");

        var response = await client.PostAsync("/api/v1/shop/buy-freeze", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<BuyFreezeResultDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }
}
