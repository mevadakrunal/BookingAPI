using Microsoft.EntityFrameworkCore;
using PropertyShowingBookingApi.Api.Data;
using PropertyShowingBookingApi.Api.Dtos;
using PropertyShowingBookingApi.Api.Errors;
using PropertyShowingBookingApi.Api.Models;

namespace PropertyShowingBookingApi.Api.Services;

public class BookingService
{
    private static readonly System.ComponentModel.DataAnnotations.EmailAddressAttribute EmailValidator = new();

    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public BookingService(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<BookingResponse> CreateBookingAsync(Guid slotId, CreateBookingRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RenterEmail))
        {
            throw new ValidationException("renterEmail is required.");
        }

        var email = request.RenterEmail.Trim().ToLowerInvariant();

        if (!EmailValidator.IsValid(email))
        {
            throw new ValidationException("renterEmail must be a valid email address.");
        }

        if (request.PartySize is < 1 or > 6)
        {
            throw new ValidationException("partySize must be between 1 and 6.");
        }

        var slot = await _db.Slots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == slotId, ct)
            ?? throw new NotFoundException($"Slot {slotId} was not found.");

        var bookingId = Guid.NewGuid();

        var inserted = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO Bookings (Id, SlotId, RenterEmail, PartySize, Status)
            SELECT {bookingId}, {slotId}, {email}, {request.PartySize}, 'Confirmed'
            WHERE
                (
                    SELECT COALESCE(SUM(PartySize), 0) FROM Bookings
                    WHERE SlotId = {slotId} AND Status IN ('Confirmed', 'Attended', 'NoShow')
                ) + {request.PartySize} <= {slot.Capacity}
                AND NOT EXISTS (
                    SELECT 1 FROM Bookings
                    WHERE SlotId = {slotId} AND RenterEmail = {email} AND Status = 'Confirmed'
                )
        ", ct);

        if (inserted == 0)
        {
            var hasActiveBooking = await _db.Bookings.AsNoTracking()
                .AnyAsync(b => b.SlotId == slotId && b.RenterEmail == email && b.Status == BookingStatus.Confirmed, ct);

            if (hasActiveBooking)
            {
                throw new ConflictException($"Renter {email} already has an active booking for this slot.");
            }

            throw new ConflictException("Not enough seats remaining on this slot.");
        }

        _db.BookingHistoryEntries.Add(new BookingHistoryEntry
        {
            BookingId = bookingId,
            FromStatus = null,
            ToStatus = BookingStatus.Confirmed,
            Actor = "renter",
            Reason = "Booking created",
            OccurredAt = _timeProvider.GetUtcNow()
        });
        await _db.SaveChangesAsync(ct);

        return new BookingResponse(bookingId, slotId, email, request.PartySize, BookingStatus.Confirmed.ToString());
    }

    public async Task<BookingResponse> TransitionAsync(Guid bookingId, TransitionRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<BookingStatus>(request.To, ignoreCase: true, out var toStatus)
            || toStatus is not (BookingStatus.Cancelled or BookingStatus.Attended or BookingStatus.NoShow))
        {
            throw new ValidationException($"'{request.To}' is not a valid transition target.");
        }

        if (!Enum.TryParse<Actor>(request.Actor, ignoreCase: true, out var actor))
        {
            throw new ValidationException($"'{request.Actor}' is not a valid actor.");
        }

        var booking = await _db.Bookings.Include(b => b.Slot)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new NotFoundException($"Booking {bookingId} was not found.");

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new ConflictException($"Booking is already {booking.Status} and cannot be changed.");
        }

        var now = _timeProvider.GetUtcNow();
        var slotStarted = now >= booking.Slot!.StartsAt;

        switch (toStatus)
        {
            case BookingStatus.Cancelled when slotStarted:
                throw new ConflictException("A booking can only be cancelled before the slot starts.");
            case BookingStatus.Attended or BookingStatus.NoShow when actor != Actor.Owner:
                throw new ConflictException("Only the owner can mark a booking as Attended or NoShow.");
            case BookingStatus.Attended or BookingStatus.NoShow when !slotStarted:
                throw new ConflictException("Attended/NoShow can only be set after the slot starts.");
        }

        var updated = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Bookings SET Status = {toStatus.ToString()}
            WHERE Id = {bookingId} AND Status = {BookingStatus.Confirmed.ToString()}
        ", ct);

        if (updated == 0)
        {
            throw new ConflictException("Booking was already transitioned by another request.");
        }

        _db.BookingHistoryEntries.Add(new BookingHistoryEntry
        {
            BookingId = bookingId,
            FromStatus = BookingStatus.Confirmed,
            ToStatus = toStatus,
            Actor = request.Actor,
            Reason = request.Reason,
            OccurredAt = now
        });
        await _db.SaveChangesAsync(ct);

        return new BookingResponse(booking.Id, booking.SlotId, booking.RenterEmail, booking.PartySize, toStatus.ToString());
    }

    public async Task<IReadOnlyList<BookingHistoryEntryResponse>> GetHistoryAsync(Guid bookingId, CancellationToken ct)
    {
        var exists = await _db.Bookings.AsNoTracking().AnyAsync(b => b.Id == bookingId, ct);
        if (!exists)
        {
            throw new NotFoundException($"Booking {bookingId} was not found.");
        }

        return await _db.BookingHistoryEntries.AsNoTracking()
            .Where(h => h.BookingId == bookingId)
            .OrderBy(h => h.OccurredAt)
            .ThenBy(h => h.Id)
            .Select(h => new BookingHistoryEntryResponse(
                h.FromStatus == null ? null : h.FromStatus.ToString(),
                h.ToStatus.ToString(),
                h.Actor,
                h.Reason,
                h.OccurredAt))
            .ToListAsync(ct);
    }
}
