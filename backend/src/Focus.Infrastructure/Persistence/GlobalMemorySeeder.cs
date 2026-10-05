using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Persistence;

public static class GlobalMemorySeeder
{
    public static async Task SeedGlobalMemoriesAsync(ApplicationDbContext context)
    {
        if (!context.Database.IsNpgsql())
        {
            await context.Database.EnsureCreatedAsync();
        }

        if (await context.AgentMemories.AnyAsync(m => m.UserId == null))
        {
            return;
        }

        var seedMemories = new List<AgentMemory>
        {
            AgentMemory.CreateGlobal(
                MemoryCategory.DeepWorkStrategy,
                "45 dakika kesintisiz odak ve ardindan 10 dakika ekransiz pasif mola ritmi, zihinsel bilis seviyesini gun boyu en yuksek verimde tutar.",
                GenerateDeterministicEmbedding("deep_work_45_10"),
                importance: 5),

            AgentMemory.CreateGlobal(
                MemoryCategory.FatigueRecovery,
                "Masada kesintisiz oturma suresi 75 dakikayi astiginda bir bardak su icmek ve esnemek, yorgunluk skorunun 70 esigini asmasini onler.",
                GenerateDeterministicEmbedding("fatigue_75min_water"),
                importance: 5),

            AgentMemory.CreateGlobal(
                MemoryCategory.ProductivityPattern,
                "Haftalik toplam calisma 35 saati astiginda aktif toparlanma (uyku, sosyal ortak alan ziyareti, kisa yuruyus) ogrenilen bilgilerin kalici hafizaya islenmesini saglar.",
                GenerateDeterministicEmbedding("weekly_recovery_social"),
                importance: 4),

            AgentMemory.CreateGlobal(
                MemoryCategory.EmotionalResponse,
                "Stresli veya enerjisi dusuk hissedilen anlarda hedefi kucultup 15 dakikalik hafif bir isinma seansi yapmak zihinsel surtunmeyi kaldirir.",
                GenerateDeterministicEmbedding("stress_warmup_15min"),
                importance: 4),

            AgentMemory.CreateGlobal(
                MemoryCategory.PersonalHabit,
                "Gece gec saatlerdeki odaklanmalarda 30 dakikayi asmayan kisa mikro-bloklar kullanmak sirkadiyen ritmi korur ve yorgunluk hissini minimize eder.",
                GenerateDeterministicEmbedding("night_micro_blocks"),
                importance: 4)
        };

        await context.AgentMemories.AddRangeAsync(seedMemories);
        await context.SaveChangesAsync();
    }

    public static float[] GenerateDeterministicEmbedding(string key)
    {
        var embedding = new float[768];
        var hash = 17;
        foreach (var c in key)
        {
            hash = hash * 31 + c;
        }

        var random = new Random(hash);
        double sumSquares = 0;
        for (int i = 0; i < 768; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            sumSquares += embedding[i] * embedding[i];
        }

        var norm = (float)Math.Sqrt(sumSquares);
        if (norm > 0)
        {
            for (int i = 0; i < 768; i++)
            {
                embedding[i] /= norm;
            }
        }

        return embedding;
    }
}
