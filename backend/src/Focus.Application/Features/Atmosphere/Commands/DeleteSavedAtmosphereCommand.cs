namespace Focus.Application.Features.Atmosphere.Commands;

using Focus.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record DeleteSavedAtmosphereCommand(Guid UserId, Guid AtmosphereId) : IRequest<bool>;

public class DeleteSavedAtmosphereCommandHandler : IRequestHandler<DeleteSavedAtmosphereCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteSavedAtmosphereCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteSavedAtmosphereCommand command, CancellationToken cancellationToken)
    {
        var item = await _context.SavedAtmospheres
            .FirstOrDefaultAsync(a => a.Id == command.AtmosphereId && a.UserId == command.UserId, cancellationToken);

        if (item == null) return false;

        _context.SavedAtmospheres.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
