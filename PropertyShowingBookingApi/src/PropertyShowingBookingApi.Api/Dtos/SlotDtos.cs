namespace PropertyShowingBookingApi.Api.Dtos;

public record CreateSlotRequest(Guid PropertyId, DateTimeOffset StartsAt, int DurationMinutes, int Capacity);

public record SlotResponse(
    Guid Id,
    Guid PropertyId,
    DateTimeOffset StartsAt,
    int DurationMinutes,
    int Capacity,
    int SeatsRemaining);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
