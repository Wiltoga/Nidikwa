namespace Nidikwa.Service;

internal class EndpointAttribute : Attribute
{
    public string Name { get; }

    public EndpointAttribute(string endpoint)
    {
        Name = endpoint;
    }
}
