using Focus.Application.Common.Exceptions;
using Focus.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Common.Extensions;

public static class ApplicationDbContextExtensions
{
    public const string DefaultConflictMessage = "Kayıt başka bir istek tarafından değiştirildi. Lütfen tekrar deneyin.";

    // Concurrency token catismasinda (ve istenirse benzersiz indeks ihlalinde) bekleyen degisiklikler atilir ve 409 uretilir
    public static async Task SaveChangesOrConflictAsync(
        this IApplicationDbContext context,
        CancellationToken cancellationToken,
        string message = DefaultConflictMessage,
        bool treatConstraintViolationAsConflict = false)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            context.DiscardPendingChanges();
            throw new ConflictException(message);
        }
        catch (DbUpdateException) when (treatConstraintViolationAsConflict)
        {
            context.DiscardPendingChanges();
            throw new ConflictException(message);
        }
    }
}
