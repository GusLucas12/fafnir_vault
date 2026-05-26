namespace fanfnir_back.Services;

public sealed record ServiceResult<T>(bool Success, T? Data, string? Error, int StatusCode)
{
    public static ServiceResult<T> Ok(T data) => new(true, data, null, StatusCodes.Status200OK);
    public static ServiceResult<T> Created(T data) => new(true, data, null, StatusCodes.Status201Created);
    public static ServiceResult<T> NoContent() => new(true, default, null, StatusCodes.Status204NoContent);
    public static ServiceResult<T> BadRequest(string error) => new(false, default, error, StatusCodes.Status400BadRequest);
    public static ServiceResult<T> NotFound(string error) => new(false, default, error, StatusCodes.Status404NotFound);
}
