namespace Focus.UnitTests.Coach;

using FluentAssertions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Atmosphere.Commands;
using Focus.Application.Features.Atmosphere.DTOs;
using Focus.Application.Features.Atmosphere.Queries;
using Focus.Application.Features.Coach.DTOs;
using Focus.Application.Features.ExternalMedia.DTOs;
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

public class AtmosphereAiServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _body;

        public StubHandler(string body)
        {
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(_body) });
    }

    private static (AtmosphereAiService Service, ApplicationDbContext Context) CreateSut(
        AiSettings? settings = null,
        HttpMessageHandler? handler = null,
        IAiUsageService? usage = null,
        int weatherCode = 1)
    {
        var context = CreateDbContext();

        var mockCoachEngine = new Mock<IFocusCoachEngine>();
        mockCoachEngine.Setup(e => e.CalculateDailyFatigueScore(It.IsAny<List<FocusSession>>(), It.IsAny<TimeZoneInfo>()))
            .Returns(20.0);

        var mockVectorMemory = new Mock<IVectorMemoryService>();
        mockVectorMemory.Setup(v => v.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[768]);
        mockVectorMemory.Setup(v => v.QueryHybridMemoriesAsync(It.IsAny<Guid>(), It.IsAny<float[]?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MemoryInsightDto>());

        var anonymizationService = new AnonymizationService();

        var mockWeather = new Mock<IWeatherService>();
        mockWeather.Setup(w => w.GetCurrentWeatherAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherReportDto(
                Temperature: 18.5,
                WindSpeed: 10.0,
                WeatherCode: weatherCode,
                WeatherCondition: "Clear",
                SuggestedTheme: "RetroStudio",
                WindowEffect: "Clear",
                SoundscapeRecommendation: "Lo-Fi Beats",
                RoomLightingColor: "#ffb86c",
                Description: "Açık ve berrak hava",
                IsDay: true,
                Time: DateTime.UtcNow));

        var apiSettings = Options.Create(settings ?? new AiSettings());
        var httpClient = handler == null ? new HttpClient() : new HttpClient(handler) { BaseAddress = new Uri(GeminiSettings.BaseUrl) };
        var logger = NullLogger<AtmosphereAiService>.Instance;

        var service = new AtmosphereAiService(
            context,
            mockCoachEngine.Object,
            mockVectorMemory.Object,
            anonymizationService,
            mockWeather.Object,
            usage ?? Mock.Of<IAiUsageService>(),
            TimeProvider.System,
            httpClient,
            apiSettings,
            logger);

        return (service, context);
    }

    [Fact]
    public void SavedAtmosphere_Create_ValidInputs_ShouldInitializeCorrectly()
    {
        var userId = Guid.NewGuid();
        var atmosphere = SavedAtmosphere.Create(
            userId: userId,
            name: "Siber Gece",
            description: "Derin siberpunk odasi",
            aesthetic: "CyberpunkNeon",
            wallColor: "#161224",
            floorColor: "#0d0a17",
            accentLightColor: "#00f0ff",
            wallPaperId: "wallpaper_slate_dark",
            floorId: "floor_parquet_oak",
            weatherEffect: "Rain",
            windowVideoQuery: "cyberpunk neon rain",
            musicGenre: "Synthwave Lofi",
            musicSearchQuery: "synthwave",
            musicVolume: 0.7f,
            ambienceType: "RainOnWindow",
            ambienceVolume: 0.6f,
            noiseType: "BrownNoise",
            noiseVolume: 0.4f,
            cutoffFrequencyHz: 450,
            textureType: "MechanicalKeyboard",
            textureVolume: 0.35f,
            recommendedMinutes: 45,
            breakMinutes: 10,
            flowShieldLevel: "Strict",
            isAiGenerated: true,
            promptUsed: "cyberpunk gece kodlama");

        atmosphere.UserId.Should().Be(userId);
        atmosphere.Name.Should().Be("Siber Gece");
        atmosphere.Aesthetic.Should().Be("CyberpunkNeon");
        atmosphere.NoiseType.Should().Be("BrownNoise");
        atmosphere.CutoffFrequencyHz.Should().Be(450);
        atmosphere.RecommendedMinutes.Should().Be(45);
        atmosphere.IsAiGenerated.Should().BeTrue();
    }

    [Fact]
    public void SavedAtmosphere_Create_EmptyName_ShouldThrowArgumentException()
    {
        var act = () => SavedAtmosphere.Create(
            userId: Guid.NewGuid(),
            name: "  ",
            description: "test",
            aesthetic: "RetroStudio",
            wallColor: "#fff",
            floorColor: "#000",
            accentLightColor: "#fff",
            wallPaperId: null,
            floorId: null,
            weatherEffect: "Clear",
            windowVideoQuery: "lofi",
            musicGenre: "Lofi",
            musicSearchQuery: "lofi",
            musicVolume: 0.5f,
            ambienceType: "Rain",
            ambienceVolume: 0.5f,
            noiseType: "BrownNoise",
            noiseVolume: 0.3f,
            cutoffFrequencyHz: 450,
            textureType: "Keyboard",
            textureVolume: 0.3f,
            recommendedMinutes: 25,
            breakMinutes: 5,
            flowShieldLevel: "Standard",
            isAiGenerated: false,
            promptUsed: null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Atmosfer adi bos olamaz*");
    }

    [Fact]
    public async Task GenerateAtmosphere_WithCyberpunkPrompt_ShouldProduceCyberpunkThemeAndBrownNoise()
    {
        var (sut, context) = CreateSut();
        var user = User.CreateGuest("Enes");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new GenerateAtmosphereRequestDto(
            Prompt: "Cyberpunk neon yağmurlu gecede kod yazmak istiyorum",
            FocusGoal: null,
            MoodTarget: null,
            PreferredAmbience: null,
            VisualAesthetic: null,
            TimeOfDay: null);

        var result = await sut.GenerateAtmosphereAsync(user.Id, request);

        result.Should().NotBeNull();
        result.VisualTheme.Aesthetic.Should().Be("CyberpunkNeon");
        result.VisualTheme.AccentLightColor.Should().Be("#00f0ff");
        result.VisualTheme.WeatherEffect.Should().Be("Rain");
        result.SoundscapeMixer.Layer3Noise.NoiseType.Should().Be("BrownNoise");
        result.SoundscapeMixer.Layer4Texture.TextureType.Should().Be("MechanicalKeyboard");
        result.RecommendedSession.DurationMinutes.Should().BeGreaterThanOrEqualTo(45);
    }

    [Fact]
    public async Task GenerateAtmosphere_WithZenPrompt_ShouldProduceZenThemeAndPinkNoise()
    {
        var (sut, context) = CreateSut();
        var user = User.CreateGuest("Selin");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new GenerateAtmosphereRequestDto(
            Prompt: "Japon zen bahçesi sakinliği ve bambu istiyorum",
            FocusGoal: null,
            MoodTarget: null,
            PreferredAmbience: null,
            VisualAesthetic: null,
            TimeOfDay: null);

        var result = await sut.GenerateAtmosphereAsync(user.Id, request);

        result.Should().NotBeNull();
        result.VisualTheme.Aesthetic.Should().Be("JapaneseZen");
        result.SoundscapeMixer.Layer3Noise.NoiseType.Should().Be("PinkNoise");
    }

    [Fact]
    public async Task GenerateAutoAtmosphere_HighFatigue_ShouldRecommendRecoveryModeAndShorterDuration()
    {
        var context = CreateDbContext();
        var user = User.CreateGuest("Mert");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // 75 yorgunluk skoru ureten mock
        var mockCoachEngine = new Mock<IFocusCoachEngine>();
        mockCoachEngine.Setup(e => e.CalculateDailyFatigueScore(It.IsAny<List<FocusSession>>(), It.IsAny<TimeZoneInfo>()))
            .Returns(75.0);

        var mockVectorMemory = new Mock<IVectorMemoryService>();
        mockVectorMemory.Setup(v => v.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[768]);
        mockVectorMemory.Setup(v => v.QueryHybridMemoriesAsync(It.IsAny<Guid>(), It.IsAny<float[]?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MemoryInsightDto>());

        var anonymizationService = new AnonymizationService();
        var mockWeather = new Mock<IWeatherService>();
        mockWeather.Setup(w => w.GetCurrentWeatherAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherReportDto?)null);

        var sut = new AtmosphereAiService(
            context,
            mockCoachEngine.Object,
            mockVectorMemory.Object,
            anonymizationService,
            mockWeather.Object,
            Mock.Of<IAiUsageService>(),
            TimeProvider.System,
            new HttpClient(),
            Options.Create(new AiSettings()),
            NullLogger<AtmosphereAiService>.Instance);

        var result = await sut.GenerateAutoAtmosphereAsync(user.Id);

        result.Should().NotBeNull();
        result.Source.Should().Be("AutoRecommended");
        result.RecommendedSession.DurationMinutes.Should().Be(20);
        result.CoachMessage.Should().Contain("yorgunluk skorun 75/100");
    }

    [Fact]
    public async Task ContributeSessionInsight_HighQualitySession_ShouldAnonymizeAndAddGlobalMemory()
    {
        var (sut, context) = CreateSut();
        var user = User.CreateGuest("Ahmet");
        context.Users.Add(user);

        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 45, 10);
        session.Complete(45 * 60, 50, 10);
        context.FocusSessions.Add(session);

        var reflection = new SessionReflection(
            session.Id,
            user.Id,
            5,
            SessionMoodAfter.Accomplished,
            "Ahmet olarak ahmet@example.com ile calistim ve cok verimli gecti");
        context.SessionReflections.Add(reflection);

        await context.SaveChangesAsync();

        var success = await sut.ContributeSessionInsightAsync(user.Id, session.Id);

        success.Should().BeTrue();

        var globalMemories = await context.AgentMemories
            .Where(m => m.UserId == null && m.Category == MemoryCategory.CollectiveWisdom)
            .ToListAsync();

        globalMemories.Should().HaveCount(1);
        globalMemories[0].Content.Should().NotContain("ahmet@example.com");
        globalMemories[0].Content.Should().Contain("[kullanici-eposta]");
        globalMemories[0].IsGlobal.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyAtmosphereCommand_ValidRequest_ShouldUpdateRoomAndPreferences()
    {
        var context = CreateDbContext();
        context.Database.EnsureCreated();
        var user = User.CreateGuest("Can");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new ApplyAtmosphereCommandHandler(context);
        var command = new ApplyAtmosphereCommand(
            user.Id,
            new ApplyAtmosphereRequestDto(
                RoomId: user.Room!.Id,
                WallPaperId: "wallpaper_brick_white",
                FloorId: "floor_parquet_oak",
                FocusMinutes: 50,
                BreakMinutes: 10,
                FlowShieldEnabled: true));

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        var updatedRoom = await context.PixelRooms.FirstAsync(r => r.Id == user.Room.Id);
        updatedRoom.WallPaperId.Should().Be("wallpaper_brick_white");

        var updatedPrefs = await context.UserPreferences.FirstAsync(p => p.UserId == user.Id);
        updatedPrefs.DefaultFocusMinutes.Should().Be(50);
        updatedPrefs.FlowShieldEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Save_And_Get_And_Delete_AtmosphereCommands_ShouldWorkCorrectly()
    {
        var context = CreateDbContext();
        var user = User.CreateGuest("Ece");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var saveHandler = new SaveAtmosphereCommandHandler(context);
        var getHandler = new GetSavedAtmospheresQueryHandler(context);
        var deleteHandler = new DeleteSavedAtmosphereCommandHandler(context);

        var generated = new GeneratedAtmosphereDto(
            AtmosphereId: Guid.NewGuid(),
            ThemeName: "Lo-Fi Akustik Salon",
            Description: "Huzurlu ahşap salon",
            VisualTheme: new VisualThemeDto("RetroStudio", "#2b2520", "#1c1815", "#f4a261", "wallpaper_brick_white", "floor_parquet_oak", "Clear", "lofi room"),
            SoundscapeMixer: new SoundscapeMixerPresetDto("Retro Mikser", new MelodyLayerDto("Lo-Fi", "lofi", 0.6f, 75), new AmbienceLayerDto("Fireplace", 0.5f), new NoiseLayerDto("BrownNoise", 0.3f, 400), new TextureLayerDto("Keyboard", 0.3f)),
            RecommendedSession: new RecommendedSessionDto(25, 5, "Standard", 4),
            CoachMessage: "Harika seans seni bekliyor",
            Source: "PromptGenerated",
            CreatedAt: DateTime.UtcNow);

        // 1. Save
        var saved = await saveHandler.Handle(new SaveAtmosphereCommand(user.Id, generated, "Özel Salonum", "gercek prompt"), CancellationToken.None);
        saved.Should().NotBeNull();
        saved.Name.Should().Be("Özel Salonum");
        saved.PromptUsed.Should().Be("gercek prompt");

        // 2. Get
        var list = await getHandler.Handle(new GetSavedAtmospheresQuery(user.Id), CancellationToken.None);
        list.Should().HaveCount(1);
        list[0].Id.Should().Be(saved.Id);

        // 3. Delete
        var deleted = await deleteHandler.Handle(new DeleteSavedAtmosphereCommand(user.Id, saved.Id), CancellationToken.None);
        deleted.Should().BeTrue();

        var listAfter = await getHandler.Handle(new GetSavedAtmospheresQuery(user.Id), CancellationToken.None);
        listAfter.Should().BeEmpty();
    }

    private static async Task<(AtmosphereAiService Sut, ApplicationDbContext Context, User User, FocusSession Session)> SeedCompletedSessionAsync(
        int quality = 5,
        string note = "ahmet@example.com ile calistim ve cok verimli gecti")
    {
        var (sut, context) = CreateSut();
        var user = User.CreateGuest("Ahmet");
        context.Users.Add(user);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 45, 10);
        session.Complete(45 * 60, 50, 10);
        context.FocusSessions.Add(session);
        context.SessionReflections.Add(new SessionReflection(session.Id, user.Id, quality, SessionMoodAfter.Accomplished, note));
        await context.SaveChangesAsync();
        return (sut, context, user, session);
    }

    [Fact]
    public async Task ContributeSessionInsight_CalledTwiceForSameSession_ContributesOnce()
    {
        var (sut, context, user, session) = await SeedCompletedSessionAsync();

        (await sut.ContributeSessionInsightAsync(user.Id, session.Id)).Should().BeTrue();
        (await sut.ContributeSessionInsightAsync(user.Id, session.Id)).Should().BeFalse();

        (await context.AgentMemories.CountAsync(m => m.SourceSessionId == session.Id)).Should().Be(1);
    }

    [Fact]
    public async Task ContributeSessionInsight_LowStoredQuality_IsRejected()
    {
        var (sut, context, user, session) = await SeedCompletedSessionAsync(quality: 2);

        (await sut.ContributeSessionInsightAsync(user.Id, session.Id)).Should().BeFalse();
        (await context.AgentMemories.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ContributeSessionInsight_ForAnotherUsersSession_IsRejected()
    {
        var (sut, context, _, session) = await SeedCompletedSessionAsync();
        var intruder = User.CreateGuest("Intruder");
        context.Users.Add(intruder);
        await context.SaveChangesAsync();

        (await sut.ContributeSessionInsightAsync(intruder.Id, session.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ContributeSessionInsight_WithoutSessionOrWithRunningSession_IsRejected()
    {
        var (sut, context) = CreateSut();
        var user = User.CreateGuest("Deniz");
        context.Users.Add(user);
        var running = FocusSession.Start(user.Id, null, SessionKind.Focus, 45, 10);
        context.FocusSessions.Add(running);
        await context.SaveChangesAsync();

        (await sut.ContributeSessionInsightAsync(user.Id, Guid.Empty)).Should().BeFalse();
        (await sut.ContributeSessionInsightAsync(user.Id, running.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ContributeSessionInsight_WhenAgentDisabled_IsRejected()
    {
        var (sut, context, user, session) = await SeedCompletedSessionAsync();
        var prefs = await context.UserPreferences.FirstAsync(p => p.UserId == user.Id);
        prefs.UpdatePreferences(25, 5, 15, false, true, null, agentEnabled: false);
        await context.SaveChangesAsync();

        (await sut.ContributeSessionInsightAsync(user.Id, session.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ContributeSessionInsight_CapsNoteLength()
    {
        var (sut, context, user, session) = await SeedCompletedSessionAsync(note: new string('a', 340));

        (await sut.ContributeSessionInsightAsync(user.Id, session.Id)).Should().BeTrue();

        var memory = await context.AgentMemories.SingleAsync();
        memory.Content.Length.Should().BeLessThan(400);
    }

    [Fact]
    public async Task GenerateAutoAtmosphere_WithRainWeatherCode_ChoosesRainAmbience()
    {
        var (sut, context) = CreateSut(weatherCode: 61);
        var user = User.CreateGuest("Yagmur");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await sut.GenerateAutoAtmosphereAsync(user.Id);

        result.SoundscapeMixer.Layer2Ambience.AmbienceType.Should().Be("RainOnWindow");
    }

    private static string BuildGeminiReply()
    {
        var atmosphere = new
        {
            themeName = "Tema\u0001",
            description = "d",
            visualTheme = new
            {
                aesthetic = "Evil",
                wallColor = "red;}",
                floorColor = "#112233",
                accentLightColor = "#zzzzzz",
                wallPaperId = "../x",
                floorId = "floor_parquet_oak",
                weatherEffect = "Meteor",
                windowVideoQuery = "q"
            },
            soundscapeMixer = new
            {
                presetName = "p",
                layer1Melody = new { genre = "g", searchQuery = "s", volume = 9, targetBpm = 9000 },
                layer2Ambience = new { ambienceType = "Nope", volume = -3 },
                layer3Noise = new { noiseType = "PinkNoise", volume = 0.5, cutoffFrequencyHz = 1 },
                layer4Texture = new { textureType = "TapeHiss", volume = 0.2 }
            },
            recommendedSession = new { durationMinutes = 99999, breakMinutes = -5, flowShieldLevel = "Strict", targetRounds = 500 },
            coachMessage = "hi"
        };

        var inner = System.Text.Json.JsonSerializer.Serialize(atmosphere);
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            candidates = new[] { new { content = new { parts = new[] { new { text = inner } } } } }
        });
    }

    [Fact]
    public async Task GenerateAtmosphere_WithGeminiOutOfRangeOutput_ClampsAndWhitelistsValues()
    {
        var settings = new AiSettings { Gemini = new GeminiSettings { ApiKey = "key" } };
        var (sut, context) = CreateSut(settings, new StubHandler(BuildGeminiReply()));
        var user = User.CreateGuest("Gemini");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await sut.GenerateAtmosphereAsync(user.Id, new GenerateAtmosphereRequestDto("sakin bir oda", null, null, null, null, null));

        result.Source.Should().Be("PromptGenerated");
        result.ThemeName.Should().Be("Tema");
        result.VisualTheme.Aesthetic.Should().Be("RetroStudio");
        result.VisualTheme.WallColor.Should().Be("#1f1d24");
        result.VisualTheme.FloorColor.Should().Be("#112233");
        result.VisualTheme.AccentLightColor.Should().Be("#ffb86c");
        result.VisualTheme.WallPaperId.Should().Be("wallpaper_brick_white");
        result.VisualTheme.WeatherEffect.Should().Be("Clear");
        result.SoundscapeMixer.Layer1Melody.Volume.Should().Be(1f);
        result.SoundscapeMixer.Layer1Melody.TargetBpm.Should().Be(200);
        result.SoundscapeMixer.Layer2Ambience.AmbienceType.Should().Be("RainOnWindow");
        result.SoundscapeMixer.Layer2Ambience.Volume.Should().Be(0f);
        result.SoundscapeMixer.Layer3Noise.CutoffFrequencyHz.Should().Be(100);
        result.RecommendedSession.DurationMinutes.Should().Be(180);
        result.RecommendedSession.BreakMinutes.Should().Be(1);
        result.RecommendedSession.TargetRounds.Should().Be(12);
    }

    [Fact]
    public async Task GenerateAtmosphere_WhenDailyBudgetExceeded_Throws()
    {
        var usage = new Mock<IAiUsageService>();
        usage.Setup(u => u.ConsumeOrThrowAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Focus.Application.Common.Exceptions.AiBudgetExceededException());
        var settings = new AiSettings { Gemini = new GeminiSettings { ApiKey = "key" } };
        var (sut, context) = CreateSut(settings, new StubHandler("{}"), usage.Object);
        var user = User.CreateGuest("Butce");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var act = () => sut.GenerateAtmosphereAsync(user.Id, new GenerateAtmosphereRequestDto("oda", null, null, null, null, null));

        await act.Should().ThrowAsync<Focus.Application.Common.Exceptions.AiBudgetExceededException>();
    }

    [Fact]
    public async Task ApplyAtmosphereCommand_ClampsMinutesAndReturnsFalseWhenNothingApplied()
    {
        var context = CreateDbContext();
        context.Database.EnsureCreated();
        var user = User.CreateGuest("Sinir");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var handler = new ApplyAtmosphereCommandHandler(context);

        var applied = await handler.Handle(new ApplyAtmosphereCommand(user.Id, new ApplyAtmosphereRequestDto(FocusMinutes: 9999, BreakMinutes: -4)), CancellationToken.None);

        applied.Should().BeTrue();
        var prefs = await context.UserPreferences.FirstAsync(p => p.UserId == user.Id);
        prefs.DefaultFocusMinutes.Should().Be(180);
        prefs.ShortBreakMinutes.Should().Be(1);

        var stranger = await handler.Handle(new ApplyAtmosphereCommand(Guid.NewGuid(), new ApplyAtmosphereRequestDto(FocusMinutes: 30)), CancellationToken.None);
        stranger.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAtmosphere_TruncatesOverlongTextToColumnLimits()
    {
        var context = CreateDbContext();
        var user = User.CreateGuest("Uzun");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var generated = new GeneratedAtmosphereDto
        {
            ThemeName = new string('t', 400),
            Description = new string('d', 900)
        };

        var saved = await new SaveAtmosphereCommandHandler(context).Handle(
            new SaveAtmosphereCommand(user.Id, generated, null, new string('p', 3000)), CancellationToken.None);

        saved.Name.Length.Should().Be(150);
        saved.Description.Length.Should().Be(500);
        saved.PromptUsed!.Length.Should().Be(1000);
    }

    [Fact]
    public async Task SaveAtmosphereValidator_RejectsMissingNestedObjects()
    {
        var validator = new SaveAtmosphereCommandValidator();
        var broken = new GeneratedAtmosphereDto { VisualTheme = null!, SoundscapeMixer = null! };

        var result = await validator.ValidateAsync(new SaveAtmosphereCommand(Guid.NewGuid(), broken));

        result.IsValid.Should().BeFalse();
    }
}
