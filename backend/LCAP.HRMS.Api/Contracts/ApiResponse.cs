namespace LCAP.HRMS.Api.Contracts;

public sealed record ApiResponse<T>(bool Success, string Message, T? Data, string TraceId,
    IReadOnlyList<string> Errors)
{
    public static ApiResponse<T> Ok(T data, string traceId, string message = "Request completed successfully.") =>
        new(true, message, data, traceId, []);

    public static ApiResponse<T> Failure(string message, string traceId, IReadOnlyList<string>? errors = null) =>
        new(false, message, default, traceId, errors ?? []);
}
