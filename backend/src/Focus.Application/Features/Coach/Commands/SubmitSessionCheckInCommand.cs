using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Coach.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Coach.Commands;

public record SubmitSessionCheckInCommand(
    Guid UserId,
    Guid SessionId,
    SessionMood Mood,
    int EnergyLevel,
    string? TargetIntent) : IRequest<SessionCheckInDto>;

public class SubmitSessionCheckInCommandHandler : IRequestHandler<SubmitSessionCheckInCommand, SessionCheckInDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IVectorMemoryService _vectorMemoryService;

    public SubmitSessionCheckInCommandHandler(
        IApplicationDbContext context,
        IVectorMemoryService vectorMemoryService)
    {
        _context = context;
        _vectorMemoryService = vectorMemoryService;
    }

    public async Task<SessionCheckInDto> Handle(SubmitSessionCheckInCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.FocusSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == request.UserId, cancellationToken);

        if (session == null)
        {
            throw new KeyNotFoundException("Gecerli bir odak seansi bulunamadi.");
        }

        var checkIn = new SessionCheckIn(
            request.SessionId,
            request.UserId,
            request.Mood,
            request.EnergyLevel,
            request.TargetIntent);

        _context.SessionCheckIns.Add(checkIn);
        await _context.SaveChangesAsync(cancellationToken);

        // Eger hedef niyeti belirtilmisse ozel hafizaya ekleyelim
        if (!string.IsNullOrWhiteSpace(request.TargetIntent))
        {
            var memoryText = $"Kullanici '{request.Mood}' ruh hali ve {request.EnergyLevel}/5 enerji seviyesiyle su hedefe odaklandi: {request.TargetIntent.Trim()}";
            var embedding = await _vectorMemoryService.GenerateEmbeddingAsync(memoryText, cancellationToken);
            await _vectorMemoryService.AddPrivateMemoryAsync(
                request.UserId,
                MemoryCategory.PersonalHabit,
                memoryText,
                embedding,
                importance: request.EnergyLevel >= 4 ? 4 : 3,
                cancellationToken: cancellationToken);
        }

        return new SessionCheckInDto(
            checkIn.Id,
            checkIn.SessionId,
            checkIn.UserId,
            checkIn.Mood,
            checkIn.EnergyLevel,
            checkIn.TargetIntent,
            checkIn.CreatedAt);
    }
}
