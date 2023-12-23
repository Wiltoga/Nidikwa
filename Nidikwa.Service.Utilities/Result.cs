namespace Nidikwa.Service.Utilities;

public enum ResultCodes
{
    Success,
    InvalidEndpoint,
    InvalidInputStructure,
    NotFound,
}

public class Result
{
    public ResultCodes Code { get; init; }
    public string? ErrorMessage { get; init; }
}

public class Result<T> : Result
{
    public T? Data { get; init; }
}
