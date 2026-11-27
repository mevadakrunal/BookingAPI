namespace PropertyShowingBookingApi.Api.Dtos;

public record CreateBookingRequest(string RenterEmail, int PartySize);

public record BookingResponse(
    Guid Id,
    Guid SlotId,
    string RenterEmail,
    int PartySize,
    string Status);

public record TransitionRequest(string To, string? Reason, string Actor);

public record BookingHistoryEntryResponse(
    string? FromStatus,
    string ToStatus,
    string? Actor,
    string? Reason,
    DateTimeOffset OccurredAt);
