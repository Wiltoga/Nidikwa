using Newtonsoft.Json;
using System.Reflection;

namespace Nidikwa.Service;

internal sealed partial class Controller : IController
{
    private readonly ILogger<Controller> logger;
    private readonly IAudioService audioService;
    private readonly JsonSerializerSettings serializerSettings;

    public Controller(
        ILogger<Controller> logger,
        IAudioService audioService,
        JsonSerializerSettings serializerSettings
    )
    {
        this.logger = logger;
        this.audioService = audioService;
        this.serializerSettings = serializerSettings;
    }
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
            var parameter = parameters.FirstOrDefault();

            if (method.ReturnType != typeof(Task<Result>))
                continue;
            if (parameter is not null)
            {
                Endpoints.Add(endpointAttribute.Name, (method.Name, (Controller controller, string arg) =>
                {
                    var deserialized = JsonConvert.DeserializeObject(arg, parameter.ParameterType, controller.serializerSettings);
                    return (Task<Result>)method.Invoke(controller, [deserialized])!;
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

    public async Task<string> HandleRequestAsync(string input)
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
            return JsonConvert.SerializeObject(InvalidInputStructure($"Invalid input structure '{input}'"), serializerSettings);
        }

        if (Endpoints.TryGetValue(enpointName, out var endpoint))
        {
            logger.LogInformation("Calling {callback}()", endpoint.Name);
            logger.LogInformation("Data : '{data}'", data);
            var result = await endpoint.Call.Invoke(this, data);
            var response = JsonConvert.SerializeObject(result, serializerSettings);
            logger.LogInformation("Response : '{response}'", response);
            return response;
        }
        else
        {
            logger.LogError("Invalid endpoint '{enpointName}'", enpointName);
            return JsonConvert.SerializeObject(InvalidEndpoint($"Invalid endpoint '{enpointName}'"), serializerSettings);
        }
    }
}
