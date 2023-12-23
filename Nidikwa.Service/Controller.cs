namespace Nidikwa.Service;

internal class Controller(
    ILogger<Controller> logger,
    IAudioService audioService
) : IController
{
    public async Task<Result> ParseInputAsync(string input)
    {
        logger.LogInformation("Recieved {input}", input);

        var result = new Result
        {
            Code = 0,
        };

        return result;
    }
}
