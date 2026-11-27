using System.Net;

namespace PropertyShowingBookingApi.Api.Errors;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                ValidationException => (HttpStatusCode.BadRequest, "Bad Request"),
                NotFoundException => (HttpStatusCode.NotFound, "Not Found"),
                ConflictException => (HttpStatusCode.Conflict, "Conflict"),
                _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
            };

            if (status == HttpStatusCode.InternalServerError)
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<ExceptionHandlingMiddleware>>();
                logger.LogError(ex, "Unhandled exception");
            }

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = $"https://httpstatuses.com/{(int)status}",
                title,
                status = (int)status,
                detail = ex.Message
            });
        }
    }
}
