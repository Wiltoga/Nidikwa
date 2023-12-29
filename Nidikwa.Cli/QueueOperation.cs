namespace Nidikwa.Cli;

[Operation("queue", "q", "Enqueues the current recording")]
internal class QueueOperation : IOperation
{
    public async Task ExecuteAsync(string[] args)
    {
        if (args.Length <= 1)
        {
            Console.WriteLine($"""
                Usage : queue
                """);
            return;
        }
        var instance = await SdkHandler.GetInstanceAsync();

        await instance.AddToQueueAsync();
    }
}
