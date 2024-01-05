using Nidikwa.Common;

namespace Nidikwa.Service.Controllerv1;

internal partial class Controller
{
    private static Result Success() => new Result { Code = ResultCodes.Success };

    private static Result<T> Success<T>(T data) => new Result<T> { Code = ResultCodes.Success, Data = data };
    private static ContentResult Success(ReadOnlyMemory<byte> data) => new ContentResult { Code = ResultCodes.Success, AdditionnalContent = data };

    private static Result NotFound(string? message = null) => new Result { Code = ResultCodes.NotFound, ErrorMessage = message };

    private static Result InvalidInputStructure(string? message = null) => new Result { Code = ResultCodes.InvalidInputStructure, ErrorMessage = message };

    private static Result InvalidState(string? message = null) => new Result { Code = ResultCodes.InvalidState, ErrorMessage = message };

    private static Result InvalidEndpoint(string? message = null) => new Result { Code = ResultCodes.InvalidEndpoint, ErrorMessage = message };
}
