namespace OrdoWiki.Web.Models;

using Exceptions;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Value { get; set; } = default!;
    public string? Error { get; set; }
    public ResponseType ResponseType { get; set; }

    public ApiResponse()
    {
    }

    public ApiResponse(T value)
    {
        Success = true;
        Value = value;
        ResponseType = ResponseType.Ok;
    }

    public override string ToString() =>
        $"ApiResponse{{Success={Success}, Error='{Error}', Value={Value?.ToString()}}}";


    public static implicit operator T(ApiResponse<T> response) =>
        response.Success
            ? response.Value
            : throw new ResponseException("Always check for response success before accessing the value.");

    public static implicit operator ApiResponse<T>(Exception exception) =>
        new() { Success = false, Error = exception.Message, ResponseType = ResponseType.InternalServerError };

    public static implicit operator ApiResponse<T>(bool success) => new() { Success = success };

    public static implicit operator bool(ApiResponse<T> response) => response.Success;
}

public enum ResponseType
{
    Ok,
    BadRequest,
    NotFound,
    Unauthorized,
    Forbidden,
    InternalServerError
}

public static class ResponseBuilder
{
    public static ApiResponse<T> Ok<T>(T value) =>
        new() { Success = true, Value = value, ResponseType = ResponseType.Ok };

    public static ApiResponse<T> BadRequest<T>(string error) => new()
        { Success = false, Error = error, ResponseType = ResponseType.BadRequest };

    public static ApiResponse<T> NotFound<T>() => new()
        { Success = false, Error = "Resource not found", ResponseType = ResponseType.NotFound };

    public static ApiResponse<T> Unauthorized<T>(string? msg = null) => new()
        { Success = false, Error = $"Unauthorized{(msg is not null ? $": {msg}" : "")}", ResponseType = ResponseType.Unauthorized };

    public static ApiResponse<T> Forbidden<T>(string? msg = null) => new()
        { Success = false, Error = $"Forbidden{(msg is not null ? $": {msg}" : "")}", ResponseType = ResponseType.Forbidden };

    public static ApiResponse<T> InternalServerError<T>(Exception ex) => new()
    {
        Success = false, Error = $"Internal server error: {ex.Message}", ResponseType = ResponseType.InternalServerError
    };
}