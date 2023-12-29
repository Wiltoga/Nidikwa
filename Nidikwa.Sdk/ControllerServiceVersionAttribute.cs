namespace Nidikwa.Sdk;

internal class ControllerServiceVersionAttribute : Attribute
{
    public ControllerServiceVersionAttribute(ushort version)
    {
        Version = version;
    }

    public ushort Version { get; }
}
