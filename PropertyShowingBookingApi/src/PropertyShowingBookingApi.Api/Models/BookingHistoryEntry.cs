namespace PropertyShowingBookingApi.Api.Models;

public class BookingHistoryEntry
{
    public long Id { get; set; }
    public Guid BookingId { get; set; }
    public BookingStatus? FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }
    public string? Actor { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
