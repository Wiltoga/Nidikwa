namespace Nidikwa.Service;

internal interface IController
{
    Task HandleRequestAsync(string input, Stream responseStream);
}
