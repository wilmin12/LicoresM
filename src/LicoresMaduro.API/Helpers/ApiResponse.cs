namespace LicoresMaduro.API.Helpers;

/// <summary>
/// Generic wrapper for all API responses.
/// Provides a consistent envelope: { Success, Message, Data, Errors }.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool     Success  { get; init; }
    public string?  Message  { get; init; }
    public T?       Data     { get; init; }
    public string[] Errors   { get; init; } = [];

    // ── Factory methods ────────────────────────────────────────────────────────

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string error, string? message = null) =>
        new() { Success = false, Message = message, Errors = [error] };

    public static ApiResponse<T> Fail(IEnumerable<string> errors, string? message = null) =>
        new() { Success = false, Message = message, Errors = errors.ToArray() };
}

/// <summary>
/// Non-generic variant for operations that return no data payload.
/// </summary>
public sealed class ApiResponse
{
    public bool     Success { get; init; }
    public string?  Message { get; init; }
    public string[] Errors  { get; init; } = [];

    public static ApiResponse Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static ApiResponse Fail(string error, string? message = null) =>
        new() { Success = false, Message = message, Errors = [error] };

    public static ApiResponse Fail(IEnumerable<string> errors, string? message = null) =>
        new() { Success = false, Message = message, Errors = errors.ToArray() };
}

/// <summary>
/// Paged result wrapper for list endpoints.
/// </summary>
public sealed class PagedResponse<T>
{
    public bool     Success     { get; init; }
    public string?  Message     { get; init; }
    public IReadOnlyList<T> Data { get; init; } = [];
    public int      Page        { get; init; }
    public int      PageSize    { get; init; }
    public int      TotalCount  { get; init; }
    public int      TotalPages  => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public static PagedResponse<T> Ok(IReadOnlyList<T> data, int page, int pageSize, int total, string? message = null) =>
        new() { Success = true, Data = data, Page = page, PageSize = pageSize, TotalCount = total, Message = message };
}
