namespace AdsManager.Application.Common;

public sealed class Result<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Details { get; init; }
    public T? Data { get; init; }

    public static Result<T> Ok(T data, string message = "OK") => new() { Success = true, Data = data, Message = message };
    public static Result<T> Fail(string message, string? details = null) => new() { Success = false, Message = message, Details = details };
}
