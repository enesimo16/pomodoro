using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Room.Commands;

public record UpdateRoomDetailsCommand(
    Guid UserId,
    string Name,
    bool IsPublic,
    int MaxVisitors = 5) : IRequest<PixelRoomDto?>;

public class UpdateRoomDetailsCommandValidator : AbstractValidator<UpdateRoomDetailsCommand>
{
    public UpdateRoomDetailsCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Oda adı zorunludur.")
            .Must(n => n.Trim().Length > 0).WithMessage("Oda adı zorunludur.")
            .MaximumLength(PixelRoom.MaxNameLength).WithMessage("Oda adı en fazla 100 karakter olabilir.");

        RuleFor(x => x.MaxVisitors)
            .InclusiveBetween(1, PixelRoom.ProMaxVisitors).WithMessage("Ziyaretçi sayısı geçersiz.");
    }
}

public class UpdateRoomDetailsCommandHandler : IRequestHandler<UpdateRoomDetailsCommand, PixelRoomDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateRoomDetailsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PixelRoomDto?> Handle(UpdateRoomDetailsCommand request, CancellationToken cancellationToken)
    {
        var isPro = await _context.Users.AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => u.IsPro)
            .FirstOrDefaultAsync(cancellationToken);

        var cap = PixelRoom.MaxVisitorsFor(isPro);
        if (request.MaxVisitors > cap)
        {
            throw new ValidationException($"Hesap türünüz için en fazla {cap} ziyaretçi ayarlanabilir.");
        }

        var room = await _context.PixelRooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OwnerUserId == request.UserId, cancellationToken);

        if (room == null)
        {
            room = PixelRoom.CreateDefault(request.UserId);
            _context.PixelRooms.Add(room);
        }

        room.UpdateDetails(request.Name.Trim(), request.IsPublic, request.MaxVisitors);
        await _context.SaveChangesAsync(cancellationToken);

        return PixelRoomDto.FromEntity(room);
    }
}
