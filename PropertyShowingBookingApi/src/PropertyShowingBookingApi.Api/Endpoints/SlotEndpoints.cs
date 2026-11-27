using PropertyShowingBookingApi.Api.Dtos;
using PropertyShowingBookingApi.Api.Services;

namespace PropertyShowingBookingApi.Api.Endpoints;

public static class SlotEndpoints
{
    public static void MapSlotEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/slots").WithTags("Slots");

        group.MapPost("/", async (CreateSlotRequest request, SlotService service, CancellationToken ct) =>
        {
            var result = await service.CreateSlotAsync(request, ct);
            return Results.Created($"/slots/{result.Id}", result);
        });

        group.MapGet("/", async (
            Guid? propertyId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int page,
            int pageSize,
            SlotService service,
            CancellationToken ct) =>
        {
            page = page == 0 ? 1 : page;
            pageSize = pageSize == 0 ? 20 : pageSize;

            var result = await service.GetSlotsAsync(propertyId, from, to, page, pageSize, ct);
            return Results.Ok(result);
        });

        group.MapPost("/{id:guid}/bookings", async (
            Guid id,
            CreateBookingRequest request,
            BookingService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateBookingAsync(id, request, ct);
            return Results.Created($"/bookings/{result.Id}", result);
        });
    }
}
