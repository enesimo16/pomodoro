using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Atmosphere.DTOs;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Atmosphere;

public class AtmosphereAiApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AtmosphereAiApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Generate_AutoRecommend_Save_List_Apply_And_Delete_Lifecycle_Succeeds()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("AtmosTester");

        // 1. Generate custom atmosphere from prompt
        var generateRequest = new
        {
            prompt = "Gece yağmurlu siberpunk çalışma odası ve derin kahverengi gürültü",
            focusGoal = "DeepCode",
            moodTarget = "Focused",
            preferredAmbience = "Rainstorm",
            visualAesthetic = "Cyberpunk"
        };

        var genResponse = await client.PostAsJsonAsync("/api/v1/ai-atmosphere/generate", generateRequest);
        genResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var generated = await genResponse.Content.ReadFromJsonAsync<GeneratedAtmosphereDto>();
        generated.Should().NotBeNull();
        generated!.ThemeName.Should().NotBeNullOrWhiteSpace();
        generated.VisualTheme.WallColor.Should().StartWith("#");
        generated.SoundscapeMixer.Layer1Melody.Volume.Should().BeInRange(0f, 1f);

        // 2. Auto-recommend atmosphere
        var autoResponse = await client.GetAsync("/api/v1/ai-atmosphere/auto-recommend");
        autoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var autoAtmos = await autoResponse.Content.ReadFromJsonAsync<GeneratedAtmosphereDto>();
        autoAtmos.Should().NotBeNull();
        autoAtmos!.ThemeName.Should().NotBeNullOrWhiteSpace();

        // 3. Save generated atmosphere
        var saveResponse = await client.PostAsJsonAsync("/api/v1/ai-atmosphere/save", new
        {
            atmosphere = generated,
            customName = "Gece Siberpunk Preset"
        });
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await saveResponse.Content.ReadFromJsonAsync<SavedAtmosphereDto>();
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Gece Siberpunk Preset");

        // 4. List saved atmospheres
        var listResponse = await client.GetAsync("/api/v1/ai-atmosphere/saved");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var savedList = await listResponse.Content.ReadFromJsonAsync<List<SavedAtmosphereDto>>();
        savedList.Should().NotBeNull();
        savedList!.Should().Contain(x => x.Id == saved.Id);

        // 5. Apply atmosphere to room & preferences
        var applyRequest = new
        {
            wallPaperId = generated.VisualTheme.WallPaperId ?? "wall_neon",
            floorId = generated.VisualTheme.FloorId ?? "floor_dark_wood",
            focusMinutes = 50,
            breakMinutes = 10,
            flowShieldEnabled = true
        };
        var applyResponse = await client.PostAsJsonAsync("/api/v1/ai-atmosphere/apply", applyRequest);
        applyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Contribute anonymized insight to collective memory
        var contributeResponse = await client.PostAsJsonAsync("/api/v1/ai-atmosphere/contribute-insight", new
        {
            plannedDurationMinutes = 50,
            focusQuality = 5,
            notes = "Yağmur ve kahverengi gürültü kombinasyonu derin odaklanmayı hızlandırdı."
        });
        contributeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7. Delete saved atmosphere
        var deleteResponse = await client.DeleteAsync($"/api/v1/ai-atmosphere/saved/{saved.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
