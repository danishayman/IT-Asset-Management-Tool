namespace ItAssetManagement.Services;

/// <summary>
/// The outcome of a write operation. Services return this instead of throwing for the
/// expected failures (not found, duplicate tag, edit conflict), which keeps controllers
/// free of try/catch and keeps exceptions meaning "something genuinely unforeseen".
/// </summary>
public record ServiceResult(bool Succeeded, string? Error = null)
{
    public static ServiceResult Ok() => new(true);

    public static ServiceResult Fail(string error) => new(false, error);
}

public record ServiceResult<T>(bool Succeeded, T? Value = default, string? Error = null)
{
    public static ServiceResult<T> Ok(T value) => new(true, value);

    public static ServiceResult<T> Fail(string error) => new(false, default, error);
}
