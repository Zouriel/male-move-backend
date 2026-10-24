namespace MaleMove.Application.Common;

public sealed record ApiError(string Message, string? Field = null, string? Code = null);

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<ApiError>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, IReadOnlyList<ApiError>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

