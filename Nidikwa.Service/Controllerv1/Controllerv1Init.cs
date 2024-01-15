using Newtonsoft.Json;
using Nidikwa.Common;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Nidikwa.Service.Controllerv1;

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

    private static Dictionary<string, (string Name, Func<Controller, string?, Task<Result>> Call)> Endpoints { get; }

    static Controller()
    {
        Endpoints = new(StringComparer.FromComparison(StringComparison.InvariantCultureIgnoreCase));

        foreach (var method in typeof(Controller).GetMethods())
        {
            var endpointAttribute = method.GetCustomAttribute<EndpointAttribute>();
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
            if (method.ReturnType.GenericTypeArguments[0] != typeof(Result)
                && method.ReturnType.GenericTypeArguments[0] != typeof(ContentResult))
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
                }
                ));
            }
            else
            {
                var ResultProperty = method.ReturnType.GetProperty(nameof(Task<object>.Result)) ?? throw new Exception("Unable to find Task.Result property");
                Endpoints.Add(endpointAttribute.Name, (method.Name, async (Controller controller, string? arg) =>
                {
                    var task = (Task)method.Invoke(controller, null)!;
                    await task.ConfigureAwait(false);
                    dynamic taskResult = task;
                    return (Result)ResultProperty.GetValue(taskResult);
                }
                ));
            }
        }
    }

    private async Task WriteStringAsync(string data, Stream stream)
    {
        var resultBytes = Encoding.UTF8.GetBytes(data);

        await stream.WriteAsync(BitConverter.GetBytes(resultBytes.Length));

        await stream.WriteAsync(resultBytes);
    }

    public async Task HandleRequestAsync(string input, Stream stream)
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
            await WriteStringAsync(JsonConvert.SerializeObject(InvalidInputStructure($"Invalid input structure '{input}'"), serializerSettings), stream);
            return;
        }

        if (Endpoints.TryGetValue(enpointName, out var endpoint))
        {
            logger.LogInformation("Calling {callback}()", endpoint.Name);
            logger.LogInformation("Data : '{data}'", data);
            Result result;
            try
            {
                result = await endpoint.Call.Invoke(this, data).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "{message}", ex.Message);
                await WriteStringAsync(JsonConvert.SerializeObject(InvalidState(ex.Message), serializerSettings), stream);
                return;
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogError(ex, "{message}", ex.Message);
                await WriteStringAsync(JsonConvert.SerializeObject(NotFound(ex.Message), serializerSettings), stream);
                return;
            }
            var response = JsonConvert.SerializeObject(result, serializerSettings);
            logger.LogInformation("Response : '{response}'", response);
            await WriteStringAsync(response, stream);
            if (result is ContentResult contentResult && contentResult.AdditionnalContent is not null)
            {
                await contentResult.AdditionnalContent.CopyToAsync(stream);
            }
        }
        else
        {
            logger.LogError("Invalid endpoint '{enpointName}'", enpointName);
            await WriteStringAsync(JsonConvert.SerializeObject(InvalidEndpoint($"Invalid endpoint '{enpointName}'"), serializerSettings), stream);
        }
    }
}
