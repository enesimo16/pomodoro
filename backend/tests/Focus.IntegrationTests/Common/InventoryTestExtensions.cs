using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Focus.IntegrationTests.Common;

public static class InventoryTestExtensions
{
    // Satin alma akisini atlayarak kullanicinin envanterine katalog esyasi ekler
    public static async Task GrantItemAsync(this CustomWebApplicationFactory factory, Guid userId, string catalogItemId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        context.UserInventoryItems.Add(new UserInventoryItem(userId, catalogItemId));
        await context.SaveChangesAsync();
    }
}
