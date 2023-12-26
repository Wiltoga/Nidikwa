namespace Nidikwa.Service;

internal interface IController
{
    ushort Version { get; }

    Task<string> HandleRequestAsync(string input);
}
