using PropertyShowingBookingApi.Api.Dtos;
using PropertyShowingBookingApi.Api.Services;

namespace PropertyShowingBookingApi.Api.Endpoints;

public static class BookingEndpoints
{
    public static void MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bookings").WithTags("Bookings");

        group.MapPost("/{id:guid}/transitions", async (
            Guid id,
            TransitionRequest request,
            BookingService service,
            CancellationToken ct) =>
        {
            var result = await service.TransitionAsync(id, request, ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}/history", async (
            Guid id,
            BookingService service,
            CancellationToken ct) =>
        {
            var result = await service.GetHistoryAsync(id, ct);
            return Results.Ok(result);
        });
    }
}
