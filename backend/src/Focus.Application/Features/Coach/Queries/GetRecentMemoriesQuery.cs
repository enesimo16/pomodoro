using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Coach.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Coach.Queries;

public record GetRecentMemoriesQuery(Guid UserId) : IRequest<List<MemoryInsightDto>>;

public class GetRecentMemoriesQueryHandler : IRequestHandler<GetRecentMemoriesQuery, List<MemoryInsightDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRecentMemoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MemoryInsightDto>> Handle(GetRecentMemoriesQuery request, CancellationToken cancellationToken)
    {
        var privateMemories = await _context.AgentMemories
            .Where(m => m.UserId == request.UserId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(10)
            .Select(m => new MemoryInsightDto(
                m.Id,
                false,
                m.Category,
                m.Content,
                m.Importance,
                1.0))
            .ToListAsync(cancellationToken);

        var globalMemories = await _context.AgentMemories
            .Where(m => m.UserId == null)
            .OrderByDescending(m => m.Importance)
            .Take(10)
            .Select(m => new MemoryInsightDto(
                m.Id,
                true,
                m.Category,
                m.Content,
                m.Importance,
                0.9))
            .ToListAsync(cancellationToken);

        var result = new List<MemoryInsightDto>();
        result.AddRange(privateMemories);
        result.AddRange(globalMemories);
        return result;
    }
}
