using ConferenceRoomBooking.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace ConferenceRoomBooking.API.Middleware;

/// <summary>
/// Глобальний обробник винятків: перетворює доменні та непередбачені помилки
/// на консистентні JSON-відповіді з правильним HTTP-статусом.
/// Захищає від витоку деталей реалізації (stack trace) у продакшн-відповідях.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            RoomNotFoundException => (HttpStatusCode.NotFound, "Зал не знайдено"),
            RoomNotAvailableException => (HttpStatusCode.Conflict, "Зал недоступний"),
            DomainException => (HttpStatusCode.BadRequest, "Помилка бізнес-логіки"),
            ArgumentException => (HttpStatusCode.BadRequest, "Некоректні вхідні дані"),
            _ => (HttpStatusCode.InternalServerError, "Внутрішня помилка сервера")
        };

        // Логуємо повний стек лише на сервері, клієнту — ні (безпека)
        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Необроблений виняток під час обробки {Path}", context.Request.Path);
        else
            _logger.LogWarning(exception, "Оброблений виняток: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            title,
            status = (int)statusCode,
            detail = exception.Message,
            // Stack trace тільки в Development — не показуємо клієнту деталі внутрішньої реалізації
            stackTrace = _env.IsDevelopment() ? exception.StackTrace : null
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}