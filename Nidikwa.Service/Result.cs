namespace Nidikwa.Service;

public static class ResultCodes
{
    public const int Success = 0;
    public const int InvalidEndpoint = 1;
    public const int InvalidInputStructure = 2;
}

internal class Result
{
    public required int Code { get; init; }
    public string? ErrorMessage { get; init; }
}

internal class Result<T> : Result
{
    public required T Data { get; init; }
}
