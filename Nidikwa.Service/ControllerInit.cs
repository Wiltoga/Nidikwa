using System.Reflection;

namespace Nidikwa.Service;

internal sealed partial class Controller(
    ILogger<Controller> logger,
    IAudioService audioService
) : IController
{
    private static Dictionary<string, (string Name, Func<Controller, string, Task<Result>> Call)> Endpoints { get; }

    static Controller()
    {
        Endpoints = new(StringComparer.FromComparison(StringComparison.InvariantCultureIgnoreCase));

        foreach (var method in typeof(Controller).GetMethods())
        {
            var endpointAttribute = method.GetCustomAttribute(typeof(EndpointAttribute)) as EndpointAttribute;
            if (endpointAttribute is null)
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length > 1)
                continue;
            if (parameters.Length == 1 && parameters[0].ParameterType != typeof(string))
                continue;
            var hasParameters = parameters.Length > 0;

            if (method.ReturnType != typeof(Task<Result>))
                continue;
            if (hasParameters)
            {
                Endpoints.Add(endpointAttribute.Name, (method.Name, (Controller controller, string arg) =>
                {
                    return (Task<Result>)method.Invoke(controller, new[] { arg })!;
                }));
            }
            else
            {
                Endpoints.Add(endpointAttribute.Name, (method.Name, (Controller controller, string arg) =>
                {
                    return (Task<Result>)method.Invoke(controller, null)!;
                }));
            }
        }
    }

    public async Task<Result> ParseInputAsync(string input)
    {
        string enpointName;
        string data;
        try
        {
            enpointName = input.Split(':')[0];
            data = input.Split(':', 2)[1];
        }
        catch (IndexOutOfRangeException)
        {
            logger.LogError("Invalid input structure '{input}'", input);
            return InvalidInputStructure($"Invalid input structure '{input}'");
        }

        if (Endpoints.TryGetValue(enpointName, out var endpoint))
        {
            logger.LogInformation("Calling '{callback}()'", endpoint.Name);
            logger.LogInformation("Data : '{data}'", data);
            return await endpoint.Call(this, data);
        }
        else
        {
            logger.LogError("Invalid endpoint '{enpointName}'", enpointName);
            return InvalidEndpoint($"Invalid endpoint '{enpointName}'");
        }
    }
}
