using Nidikwa.Cli;

IOperation calledOperation;

if (args.Length > 0)
{
    var name = args[0];

    var constructor = IOperation.AllOperations.FirstOrDefault(operation => operation.Metadata.FullName == name).Constructor ?? IOperation.AllOperations.FirstOrDefault(operation => operation.Metadata.ShortName == name).Constructor;

    if (constructor is null)
    {
        Console.WriteLine("Invalid argument");
        return;
    }

    calledOperation = constructor();
}
else
{
    var constructor = IOperation.AllOperations.First(operation => operation.Metadata.IsDefault).Constructor;

    calledOperation = constructor();

    Console.WriteLine("Usage : Nidikwa.CLI.exe <operation name> [<operation parameters>]");
    Console.WriteLine();
}

await calledOperation.ExecuteAsync(args.Skip(1).ToArray());
