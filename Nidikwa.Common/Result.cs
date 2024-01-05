using System.Text.Json.Serialization;

namespace Nidikwa.Common;

public enum ResultCodes
{
    Success,
    InvalidEndpoint,
    InvalidInputStructure,
    NotFound,
    InvalidState,
    Timeout,
    NoResponse,
    NotConnected,
    Disconnected,
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
public class ContentResult : Result
{
    [JsonIgnore]
    public ReadOnlyMemory<byte> AdditionnalContent { get; set; }
}
