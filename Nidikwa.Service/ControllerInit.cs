using Newtonsoft.Json;
using System.Reflection;
using Nidikwa.Service.Utilities;

namespace Nidikwa.Service;

internal sealed partial class Controller : IController
{
    public ushort Version => 1;

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
    private static Dictionary<string, (string Name, Func<Controller, string?, Task<Result>> Call)> Endpoints { get; }

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
            if (!method.ReturnType.IsGenericType)
                continue;
            if (method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
                continue;
            if (method.ReturnType.GenericTypeArguments[0] != typeof(Result))
            {
                if (method.ReturnType.GenericTypeArguments[0].IsGenericType)
                {
                    if (method.ReturnType.GenericTypeArguments[0].GetGenericTypeDefinition() != typeof(Result<>))
                        continue;
                }
                else
                {
                    continue;
                }
            }
            if (parameter is not null)
            {
                Endpoints.Add(endpointAttribute.Name, (method.Name, (Controller controller, string? arg) =>
                {
                    if (arg is null)
                        throw new ArgumentNullException(nameof(arg));
                    var deserialized = JsonConvert.DeserializeObject(arg, parameter.ParameterType, controller.serializerSettings);
                    return (Task<Result>)method.Invoke(controller, [deserialized])!;
                }));
            }
            else
            {
                var ResultProperty = method.ReturnType.GetProperty(nameof(Task<object>.Result)) ?? throw new Exception("Unable to find Task.Result property");
                Endpoints.Add(endpointAttribute.Name, (method.Name, async (Controller controller, string? arg) =>
                {
                    var task = (Task)method.Invoke(controller, null)!;
                    await task;
                    dynamic taskResult = task;
                    return (Result)ResultProperty.GetValue(taskResult);
                }));
            }
        }
    }

    public async Task<string> HandleRequestAsync(string input)
    {
        string enpointName;
        string? data = null;
        try
        {
            var parts = input.Split(':', 2);
            enpointName = parts[0];
            if (parts.Length > 1)
            {
                data = parts[1];
            }
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
            Result result;
            try
            {
                result = await endpoint.Call.Invoke(this, data);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "{message}", ex.Message);
                return JsonConvert.SerializeObject(InvalidState(ex.Message), serializerSettings);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogError(ex, "{message}", ex.Message);
                return JsonConvert.SerializeObject(NotFound(ex.Message), serializerSettings);
            }
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
