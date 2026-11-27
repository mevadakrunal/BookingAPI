using Microsoft.EntityFrameworkCore;
using PropertyShowingBookingApi.Api.Data;
using PropertyShowingBookingApi.Api.Dtos;
using PropertyShowingBookingApi.Api.Errors;
using PropertyShowingBookingApi.Api.Models;

namespace PropertyShowingBookingApi.Api.Services;

public class SlotService
{
    private static readonly BookingStatus[] SeatHoldingStatuses =
    {
        BookingStatus.Confirmed, BookingStatus.Attended, BookingStatus.NoShow
    };

    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public SlotService(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<SlotResponse> CreateSlotAsync(CreateSlotRequest request, CancellationToken ct)
    {
        if (request.StartsAt <= _timeProvider.GetUtcNow())
        {
            throw new ValidationException("startsAt must be in the future.");
        }

        if (request.DurationMinutes is < 15 or > 120)
        {
            throw new ValidationException("durationMinutes must be between 15 and 120.");
        }

        if (request.Capacity is < 1 or > 20)
        {
            throw new ValidationException("capacity must be between 1 and 20.");
        }

        if (request.PropertyId == Guid.Empty)
        {
            throw new ValidationException("propertyId is required.");
        }

        var slot = new Slot
        {
            PropertyId = request.PropertyId,
            StartsAt = request.StartsAt,
            DurationMinutes = request.DurationMinutes,
            Capacity = request.Capacity
        };

        _db.Slots.Add(slot);
        await _db.SaveChangesAsync(ct);

        return ToResponse(slot, 0);
    }

    public async Task<PagedResult<SlotResponse>> GetSlotsAsync(
        Guid? propertyId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1)
        {
            throw new ValidationException("page must be at least 1.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ValidationException("pageSize must be between 1 and 100.");
        }

        if (from is not null && to is not null && from > to)
        {
            throw new ValidationException("from must not be after to.");
        }

        var query = _db.Slots.AsNoTracking().AsQueryable();

        if (propertyId is not null)
        {
            query = query.Where(s => s.PropertyId == propertyId);
        }

        if (from is not null)
        {
            query = query.Where(s => s.StartsAt >= from);
        }

        if (to is not null)
        {
            query = query.Where(s => s.StartsAt <= to);
        }

        var totalCount = await query.CountAsync(ct);

        var slots = await query
            .OrderBy(s => s.StartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                Slot = s,
                SeatsUsed = s.Bookings
                    .Where(b => SeatHoldingStatuses.Contains(b.Status))
                    .Sum(b => (int?)b.PartySize) ?? 0
            })
            .ToListAsync(ct);

        var items = slots.Select(x => ToResponse(x.Slot, x.SeatsUsed)).ToList();

        return new PagedResult<SlotResponse>(items, page, pageSize, totalCount);
    }

    private static SlotResponse ToResponse(Slot slot, int seatsUsed) => new(
        slot.Id,
        slot.PropertyId,
        slot.StartsAt,
        slot.DurationMinutes,
        slot.Capacity,
        slot.Capacity - seatsUsed);
}
