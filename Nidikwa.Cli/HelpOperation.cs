namespace Nidikwa.Cli;

[Operation("help", "h", """
    Displays the help panel, giving informations about the available operations
    """, true)]
internal class HelpOperation : IOperation
{
    public Task ExecuteAsync(string host, int port, string[] args)
    {
        if (args.Length == 0)
            args = IOperation.AllOperations.Select(operation => operation.Metadata.FullName).ToArray();

        foreach (string arg in args)
        {
            var operation = IOperation.AllOperations.FirstOrDefault(operation => operation.Metadata.FullName == arg).Metadata ?? IOperation.AllOperations.FirstOrDefault(operation => operation.Metadata.ShortName == arg).Metadata;

            if (operation is null)
                continue;

            Console.WriteLine($"* {operation.FullName} ({operation.ShortName}) : {operation.HelpInfos}");
        }

        return Task.CompletedTask;
    }
}
