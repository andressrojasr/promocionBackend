namespace PromocionBackend.Application.Common;

/// <summary>
/// Envelope estándar de respuesta, compatible con el formato que ya consume el frontend.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public object? Errors { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public static ApiResponse<T> Ok(T data, string message = "Operación exitosa") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, object? errors = null) =>
        new() { Success = false, Data = default, Message = message, Errors = errors };
}
