namespace PropertyShowingBookingApi.Api.Models;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SlotId { get; set; }
    public Slot? Slot { get; set; }
    public string RenterEmail { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    public List<BookingHistoryEntry> History { get; set; } = new();
}
