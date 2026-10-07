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

    private static (AtmosphereAiService Service, ApplicationDbContext Context) CreateSut()
    {
        var context = CreateDbContext();

        var mockCoachEngine = new Mock<IFocusCoachEngine>();
        mockCoachEngine.Setup(e => e.CalculateDailyFatigueScore(It.IsAny<List<FocusSession>>()))
            .Returns(20.0);

        var mockVectorMemory = new Mock<IVectorMemoryService>();
        mockVectorMemory.Setup(v => v.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[768]);
        mockVectorMemory.Setup(v => v.QueryHybridMemoriesAsync(It.IsAny<Guid>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MemoryInsightDto>());

        var anonymizationService = new AnonymizationService();

        var mockWeather = new Mock<IWeatherService>();
        mockWeather.Setup(w => w.GetCurrentWeatherAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherReportDto(
                Temperature: 18.5,
                WindSpeed: 10.0,
                WeatherCode: 1,
                WeatherCondition: "Clear",
                SuggestedTheme: "RetroStudio",
                WindowEffect: "Clear",
                SoundscapeRecommendation: "Lo-Fi Beats",
                RoomLightingColor: "#ffb86c",
                Description: "Açık ve berrak hava",
                IsDay: true,
                Time: DateTime.UtcNow));

        var apiSettings = Options.Create(new ExternalApiSettings());
        var httpClient = new HttpClient();
        var logger = NullLogger<AtmosphereAiService>.Instance;

        var service = new AtmosphereAiService(
            context,
            mockCoachEngine.Object,
            mockVectorMemory.Object,
            anonymizationService,
            mockWeather.Object,
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
        mockCoachEngine.Setup(e => e.CalculateDailyFatigueScore(It.IsAny<List<FocusSession>>()))
            .Returns(75.0);

        var mockVectorMemory = new Mock<IVectorMemoryService>();
        mockVectorMemory.Setup(v => v.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[768]);
        mockVectorMemory.Setup(v => v.QueryHybridMemoriesAsync(It.IsAny<Guid>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
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
            new HttpClient(),
            Options.Create(new ExternalApiSettings()),
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
        var user = User.CreateGuest("Can");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new ApplyAtmosphereCommandHandler(context);
        var command = new ApplyAtmosphereCommand(
            user.Id,
            new ApplyAtmosphereRequestDto(
                RoomId: user.Room!.Id,
                WallPaperId: "wallpaper_slate_dark",
                FloorId: "floor_parquet_oak",
                FocusMinutes: 50,
                BreakMinutes: 10,
                FlowShieldEnabled: true));

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        var updatedRoom = await context.PixelRooms.FirstAsync(r => r.Id == user.Room.Id);
        updatedRoom.WallPaperId.Should().Be("wallpaper_slate_dark");

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
        var saved = await saveHandler.Handle(new SaveAtmosphereCommand(user.Id, generated, "Özel Salonum"), CancellationToken.None);
        saved.Should().NotBeNull();
        saved.Name.Should().Be("Özel Salonum");

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
}
