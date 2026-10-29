using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Focus.UnitTests.Common;

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        return CreateShared(Guid.NewGuid().ToString());
    }

    // Ayni veritabani adini paylasan birden fazla context es zamanli istekleri taklit eder
    public static ApplicationDbContext CreateShared(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static ThrowingSaveDbContext CreateThrowingOnSave(string databaseName, Func<Exception>? exceptionFactory = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new ThrowingSaveDbContext(options, exceptionFactory ?? (() => new InvalidOperationException()));
        context.Database.EnsureCreated();
        return context;
    }
}

public sealed class ThrowingSaveDbContext : ApplicationDbContext
{
    private readonly Func<Exception> _exceptionFactory;

    public ThrowingSaveDbContext(DbContextOptions<ApplicationDbContext> options, Func<Exception> exceptionFactory)
        : base(options)
    {
        _exceptionFactory = exceptionFactory;
    }

    public bool ThrowOnSave { get; set; }

    // Kayittan hemen once calisir; baska bir istegin ayni anda commit etmesini taklit eder
    public Func<Task>? BeforeSave { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (BeforeSave != null)
        {
            var hook = BeforeSave;
            BeforeSave = null;
            await hook();
        }

        if (ThrowOnSave) throw _exceptionFactory();
        return await base.SaveChangesAsync(cancellationToken);
    }
}
