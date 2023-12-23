namespace Nidikwa.Service;

internal interface IController
{
    Task<Result> ParseInputAsync(string input);
}
