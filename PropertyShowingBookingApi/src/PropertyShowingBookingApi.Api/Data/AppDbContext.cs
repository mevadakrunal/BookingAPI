using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PropertyShowingBookingApi.Api.Models;

namespace PropertyShowingBookingApi.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingHistoryEntry> BookingHistoryEntries => Set<BookingHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Slot>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.StartsAt).IsRequired().HasConversion(DateTimeOffsetToTicksConverter);
            entity.HasIndex(s => new { s.PropertyId, s.StartsAt });
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.RenterEmail).IsRequired();
            entity.Property(b => b.Status).HasConversion<string>();
            entity.HasOne(b => b.Slot)
                  .WithMany(s => s.Bookings)
                  .HasForeignKey(b => b.SlotId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(b => new { b.SlotId, b.RenterEmail });
        });

        modelBuilder.Entity<BookingHistoryEntry>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.FromStatus).HasConversion<string>();
            entity.Property(h => h.ToStatus).HasConversion<string>();
            entity.Property(h => h.OccurredAt).HasConversion(DateTimeOffsetToTicksConverter);
            entity.HasOne<Booking>()
                  .WithMany(b => b.History)
                  .HasForeignKey(h => h.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static readonly ValueConverter<DateTimeOffset, long> DateTimeOffsetToTicksConverter =
        new(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));
}
