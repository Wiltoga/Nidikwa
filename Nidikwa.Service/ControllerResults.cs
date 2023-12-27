﻿using Nidikwa.Service.Utilities;

namespace Nidikwa.Service;

internal partial class Controller
{
    private static Result Success() => new Result { Code = ResultCodes.Success };

    private static Result<T> Success<T>(T data) => new Result<T> { Code = ResultCodes.Success, Data = data };

    private static Result NotFound(string? message = null) => new Result { Code = ResultCodes.NotFound, ErrorMessage = message };

    private static Result InvalidInputStructure(string? message = null) => new Result { Code = ResultCodes.InvalidInputStructure, ErrorMessage = message };

    private static Result InvalidState(string? message = null) => new Result { Code = ResultCodes.InvalidState, ErrorMessage = message };

    private static Result InvalidEndpoint(string? message = null) => new Result { Code = ResultCodes.InvalidEndpoint, ErrorMessage = message };
}
