using System.Reflection;

namespace Nidikwa.Service;

internal class ControllerVersionAttribute : Attribute
{
    public ControllerVersionAttribute(ushort version)
    {
        Version = version;
    }

    public ushort Version { get; }
}

internal static class ControllerVersions
{
    private static readonly Dictionary<ushort, Type> versions;
    public static IServiceCollection AddControllers(this IServiceCollection services)
    {
        foreach (var type in versions)
        {
            services = services.AddKeyedScoped(typeof(IController), type.Key, type.Value);
        }
        return services;
    }
    public static ushort[] Versions => versions.Keys.ToArray();

    static ControllerVersions()
    {
        versions = new();

        foreach (var controllerType in (from type in typeof(ControllerVersions).Assembly.GetTypes()
                                        where type.GetInterfaces().Contains(typeof(IController))
                                        select type))
        {
            var attribute = controllerType.GetCustomAttribute<ControllerVersionAttribute>();
            if (attribute is null)
                continue;

            versions.Add(attribute.Version, controllerType);
        }
    }
}
