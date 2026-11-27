using Microsoft.EntityFrameworkCore;
using PropertyShowingBookingApi.Api.Data;
using PropertyShowingBookingApi.Api.Endpoints;
using PropertyShowingBookingApi.Api.Errors;
using PropertyShowingBookingApi.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing required configuration: ConnectionStrings:Default.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<SlotService>();
builder.Services.AddScoped<BookingService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.MapSlotEndpoints();
app.MapBookingEndpoints();

app.Run();

public partial class Program
{
}
