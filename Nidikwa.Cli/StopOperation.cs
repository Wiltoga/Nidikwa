namespace Nidikwa.Cli;

[Operation("stop", "s", "Stops the current recording")]
internal class StopOperation : IOperation
{
    public async Task ExecuteAsync(string[] args)
    {
        if (args.Length <= 1)
        {
            Console.WriteLine($"""
                Usage : stop
                """);
            return;
        }
        var instance = await SdkHandler.GetInstanceAsync();

        await instance.StopRecordingAsync();
    }
}
