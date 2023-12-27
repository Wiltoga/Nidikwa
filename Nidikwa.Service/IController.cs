namespace Nidikwa.Service;

internal interface IController
{
    Task<string> HandleRequestAsync(string input);
}
