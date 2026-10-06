using Focus.Application.Common.Interfaces;
using Focus.Application.Features.AdminAnalytics.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

[Route("api/v1/admin/analytics")]
public class AdminAnalyticsController : BaseApiController
{
    private readonly IAdminAnalyticsService _analyticsService;

    public AdminAnalyticsController(IAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Platform genel performans göstergelerini, günlük (DAU), haftalık (WAU), aylık (MAU) aktif kullanıcı sayılarını ve seans tamamlanma oranlarını getirir.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(PlatformOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetOverviewAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Kullanıcı seviye dağılımını, toplam avatar sayısını ve en yüksek XP ve odak serisine sahip liderlik tablosunu getirir.
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(UserAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserAnalytics(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetUserAnalyticsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Dolaşımdaki toplam Focus Coin miktarını, kazanılan/harcanan coin hacmini ve mağazada en çok satın alınan eşyaları getirir.
    /// </summary>
    [HttpGet("economy")]
    [ProducesResponseType(typeof(EconomyAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEconomyAnalytics(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetEconomyAnalyticsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Veritabanı yanıt süresini, Redis önbellek durumunu, RAG vektör hafıza adetlerini ve aktif oda sayılarını sorgulayarak sistem sağlığını denetler.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealthDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemHealth(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetSystemHealthAsync(cancellationToken);
        return Ok(result);
    }
}
