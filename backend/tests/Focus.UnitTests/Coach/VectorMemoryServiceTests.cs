using System.Net;
using FluentAssertions;
using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Infrastructure.Common;
using Focus.Infrastructure.Persistence;
using Focus.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Focus.UnitTests.Coach;

public class VectorMemoryServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body = "{}")
        {
            _status = status;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_body) });
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static VectorMemoryService CreateService(ApplicationDbContext context, HttpStatusCode status = HttpStatusCode.OK, string body = "{}", string apiKey = "key")
    {
        var settings = new AiSettings { Gemini = new GeminiSettings { ApiKey = apiKey, EmbeddingDimensions = 4 } };
        var client = new HttpClient(new StubHandler(status, body)) { BaseAddress = new Uri(GeminiSettings.BaseUrl) };
        return new VectorMemoryService(context, client, Options.Create(settings), NullLogger<VectorMemoryService>.Instance);
    }

    [Fact]
    public async Task GenerateEmbedding_WithoutApiKey_ReturnsNull()
    {
        var service = CreateService(CreateContext(), apiKey: "");

        (await service.GenerateEmbeddingAsync("merhaba")).Should().BeNull();
    }

    [Fact]
    public async Task GenerateEmbedding_WhenApiFails_ReturnsNull()
    {
        var service = CreateService(CreateContext(), HttpStatusCode.InternalServerError);

        (await service.GenerateEmbeddingAsync("merhaba")).Should().BeNull();
    }

    [Fact]
    public async Task GenerateEmbedding_NormalizesReturnedVector()
    {
        var service = CreateService(CreateContext(), body: """{"embedding":{"values":[3,4,0,0]}}""");

        var embedding = await service.GenerateEmbeddingAsync("merhaba");

        embedding.Should().NotBeNull();
        embedding![0].Should().BeApproximately(0.6f, 1e-5f);
        embedding[1].Should().BeApproximately(0.8f, 1e-5f);
    }

    [Fact]
    public async Task GenerateEmbedding_WithWrongDimensions_ReturnsNull()
    {
        var service = CreateService(CreateContext(), body: """{"embedding":{"values":[1,2]}}""");

        (await service.GenerateEmbeddingAsync("merhaba")).Should().BeNull();
    }

    [Fact]
    public async Task QueryHybridMemories_ReportsRealSimilarityAndSkipsMissingEmbeddingScore()
    {
        var context = CreateContext();
        var userId = Guid.NewGuid();
        context.AgentMemories.Add(AgentMemory.CreatePrivate(userId, MemoryCategory.PersonalHabit, "vektorlu", [1f, 0f, 0f, 0f], 5));
        context.AgentMemories.Add(AgentMemory.CreateGlobal(MemoryCategory.DeepWorkStrategy, "vektorsuz", null, 5));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.QueryHybridMemoriesAsync(userId, [1f, 0f, 0f, 0f], topK: 3);

        result.Single(r => !r.IsGlobal).RelevanceScore.Should().BeApproximately(1.0, 1e-6);
        result.Single(r => r.IsGlobal).RelevanceScore.Should().Be(0.0);
    }

    [Fact]
    public async Task Seeder_WithoutEmbeddingService_SeedsMemoriesWithoutVectors()
    {
        var context = CreateContext();
        var vector = new Mock<IVectorMemoryService>();
        vector.Setup(v => v.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((float[]?)null);

        await GlobalMemorySeeder.SeedGlobalMemoriesAsync(context, vector.Object);

        var memories = await context.AgentMemories.ToListAsync();
        memories.Should().HaveCount(5);
        memories.Should().OnlyContain(m => m.Embedding == null);
    }

    [Fact]
    public async Task Seeder_BackfillsMissingEmbeddingsWhenServiceAvailable()
    {
        var context = CreateContext();
        var vector = new Mock<IVectorMemoryService>();
        vector.Setup(v => v.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((float[]?)null);
        await GlobalMemorySeeder.SeedGlobalMemoriesAsync(context, vector.Object);

        vector.Reset();
        vector.Setup(v => v.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([0.5f, 0.5f]);
        await GlobalMemorySeeder.SeedGlobalMemoriesAsync(context, vector.Object);

        (await context.AgentMemories.ToListAsync()).Should().OnlyContain(m => m.Embedding != null);
        (await context.AgentMemories.CountAsync()).Should().Be(5);
    }
}
