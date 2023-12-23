namespace Nidikwa.Service;

internal class Result
{
    public required int Code { get; init; }
    public string? ErrorMessage { get; init; }
}

internal class Result<T> : Result where T : class
{
    public T? Data { get; init; }
}
