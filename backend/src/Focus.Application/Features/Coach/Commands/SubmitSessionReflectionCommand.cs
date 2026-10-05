using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Coach.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Coach.Commands;

public record SubmitSessionReflectionCommand(
    Guid UserId,
    Guid SessionId,
    int FocusQuality,
    SessionMoodAfter MoodAfter,
    string? DistractionNote) : IRequest<SessionReflectionDto>;

public class SubmitSessionReflectionCommandHandler : IRequestHandler<SubmitSessionReflectionCommand, SessionReflectionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IVectorMemoryService _vectorMemoryService;
    private readonly IAnonymizationService _anonymizationService;

    public SubmitSessionReflectionCommandHandler(
        IApplicationDbContext context,
        IVectorMemoryService vectorMemoryService,
        IAnonymizationService anonymizationService)
    {
        _context = context;
        _vectorMemoryService = vectorMemoryService;
        _anonymizationService = anonymizationService;
    }

    public async Task<SessionReflectionDto> Handle(SubmitSessionReflectionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.FocusSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == request.UserId, cancellationToken);

        if (session == null)
        {
            throw new KeyNotFoundException("Gecerli bir odak seansi bulunamadi.");
        }

        var reflection = new SessionReflection(
            request.SessionId,
            request.UserId,
            request.FocusQuality,
            request.MoodAfter,
            request.DistractionNote);

        _context.SessionReflections.Add(reflection);
        await _context.SaveChangesAsync(cancellationToken);

        var durationMin = (session.NetDurationSeconds > 0 ? session.NetDurationSeconds : session.FocusMinutes * 60) / 60;
        var note = string.IsNullOrWhiteSpace(request.DistractionNote) ? "Not girilmedi" : request.DistractionNote.Trim();

        // 1. Kisisel Vektor Havuzuna Ekleme (User-Specific Memory)
        var privateMemoryContent = $"{durationMin} dakikalik seans sonu degeri: Odak {request.FocusQuality}/5, His '{request.MoodAfter}'. Not: {note}";
        var privateEmbedding = await _vectorMemoryService.GenerateEmbeddingAsync(privateMemoryContent, cancellationToken);
        await _vectorMemoryService.AddPrivateMemoryAsync(
            request.UserId,
            MemoryCategory.ProductivityPattern,
            privateMemoryContent,
            privateEmbedding,
            importance: request.FocusQuality,
            cancellationToken: cancellationToken);

        // 2. Kolektif Anonim Vektor Havuzuna Ekleme (Global Anonymized Wisdom)
        // Eger yuksek odak veya ogretici bir mola deneyimi varsa kisisel verileri temizleyip havuza kat
        if (request.FocusQuality >= 4 && !string.IsNullOrWhiteSpace(request.DistractionNote))
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            var sanitizedNote = _anonymizationService.AnonymizeAndGeneralize(request.DistractionNote, user?.DisplayName);

            if (!string.IsNullOrWhiteSpace(sanitizedNote))
            {
                var globalContent = $"{durationMin} dakikalik odaklanmada '{request.MoodAfter}' deneyimi saglayan strateji: {sanitizedNote}";
                var globalEmbedding = await _vectorMemoryService.GenerateEmbeddingAsync(globalContent, cancellationToken);
                await _vectorMemoryService.AddGlobalMemoryAsync(
                    MemoryCategory.DeepWorkStrategy,
                    globalContent,
                    globalEmbedding,
                    importance: 4,
                    cancellationToken: cancellationToken);
            }
        }

        return new SessionReflectionDto(
            reflection.Id,
            reflection.SessionId,
            reflection.UserId,
            reflection.FocusQuality,
            reflection.MoodAfter,
            reflection.DistractionNote,
            reflection.CreatedAt);
    }
}
