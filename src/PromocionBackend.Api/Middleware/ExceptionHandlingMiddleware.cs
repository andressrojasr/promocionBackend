using System.Text.Json;
using PromocionBackend.Application.Common;

namespace PromocionBackend.Api.Middleware;

/// <summary>
/// Traduce las excepciones de negocio (<see cref="AppException"/>) al envelope
/// <see cref="ApiResponse{T}"/> con el código HTTP correspondiente, y oculta los
/// detalles de errores inesperados.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException exception)
        {
            await WriteResponseAsync(context, exception.StatusCode, exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error no controlado en {Path}", context.Request.Path);
            await WriteResponseAsync(context, StatusCodes.Status500InternalServerError,
                "Ocurrió un error interno. Intente nuevamente más tarde.");
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(ApiResponse<object>.Fail(message), AppJson.Options);
        await context.Response.WriteAsync(payload);
    }
}
