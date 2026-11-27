namespace PropertyShowingBookingApi.Api.Models;

public class Slot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PropertyId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }

    public List<Booking> Bookings { get; set; } = new();

    public DateTimeOffset EndsAt => StartsAt.AddMinutes(DurationMinutes);
}
