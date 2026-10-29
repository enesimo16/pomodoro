using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Persistence;

public static class GlobalMemorySeeder
{
    private const int BackfillBatchSize = 50;

    private static readonly (MemoryCategory Category, string Content, int Importance)[] SeedStrategies =
    [
        (MemoryCategory.DeepWorkStrategy,
            "45 dakika kesintisiz odak ve ardindan 10 dakika ekransiz pasif mola ritmi, zihinsel bilis seviyesini gun boyu en yuksek verimde tutar.", 5),
        (MemoryCategory.FatigueRecovery,
            "Masada kesintisiz oturma suresi 75 dakikayi astiginda bir bardak su icmek ve esnemek, yorgunluk skorunun 70 esigini asmasini onler.", 5),
        (MemoryCategory.ProductivityPattern,
            "Haftalik toplam calisma 35 saati astiginda aktif toparlanma (uyku, sosyal ortak alan ziyareti, kisa yuruyus) ogrenilen bilgilerin kalici hafizaya islenmesini saglar.", 4),
        (MemoryCategory.EmotionalResponse,
            "Stresli veya enerjisi dusuk hissedilen anlarda hedefi kucultup 15 dakikalik hafif bir isinma seansi yapmak zihinsel surtunmeyi kaldirir.", 4),
        (MemoryCategory.PersonalHabit,
            "Gece gec saatlerdeki odaklanmalarda 30 dakikayi asmayan kisa mikro-bloklar kullanmak sirkadiyen ritmi korur ve yorgunluk hissini minimize eder.", 4)
    ];

    public static async Task SeedGlobalMemoriesAsync(
        ApplicationDbContext context,
        IVectorMemoryService vectorMemoryService,
        CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsNpgsql())
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (!await context.AgentMemories.AnyAsync(m => m.UserId == null, cancellationToken))
        {
            foreach (var (category, content, importance) in SeedStrategies)
            {
                // Gomme servisi yapilandirilmamissa hafiza vektorsuz kaydedilir, sonraki acilista tamamlanir
                var embedding = await vectorMemoryService.GenerateEmbeddingAsync(content, cancellationToken);
                context.AgentMemories.Add(AgentMemory.CreateGlobal(category, content, embedding, importance));
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        await BackfillMissingEmbeddingsAsync(context, vectorMemoryService, cancellationToken);
    }

    private static async Task BackfillMissingEmbeddingsAsync(
        ApplicationDbContext context,
        IVectorMemoryService vectorMemoryService,
        CancellationToken cancellationToken)
    {
        var pending = await context.AgentMemories
            .Where(m => m.Embedding == null)
            .OrderByDescending(m => m.Importance)
            .Take(BackfillBatchSize)
            .ToListAsync(cancellationToken);

        var updated = false;
        foreach (var memory in pending)
        {
            var embedding = await vectorMemoryService.GenerateEmbeddingAsync(memory.Content, cancellationToken);
            if (embedding == null)
            {
                break;
            }

            memory.UpdateEmbedding(embedding);
            updated = true;
        }

        if (updated)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
