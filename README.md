# Booking API

An HTTP API for property owners to publish showing slots and renters to book a spot on them, built with ASP.NET Core minimal APIs and EF Core on .NET 8.

## Running it

Requires the .NET 8 SDK.

```bash
dotnet run --project src/PropertyShowingBookingApi.Api
```


## Persistence

**SQLite**, via `Microsoft.EntityFrameworkCore.Sqlite`. Chosen over EF Core InMemory because the overbooking requirement needs a real transactional/locking story to verify — InMemory doesn't model database locking at all, so a test against it would prove nothing about concurrency safety. SQLite needs no external service (unlike SQL Server in Docker), which fit the time budget, while still being a real relational database with real write-locking semantics.

## Endpoints implemented

All five from the spec: `POST /slots`, `GET /slots`, `POST /slots/{id}/bookings`, `POST /bookings/{id}/transitions`, `GET /bookings/{id}/history`.

## What I skipped

Due to short on time, I skipped adding test cases. If time permits, I will add test cases.

## AI use

Using AI I had created readme file and fix date from and to bug in filter.
