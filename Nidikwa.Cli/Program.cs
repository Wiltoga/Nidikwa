using Nidikwa.Cli;

IOperation calledOperation;

var host = "localhost";
var port = 17854;

while (args.Length > 0)
{
    if (args[0] == "--host")
    {
        host = args[1];
        args = args[2..];
    }
    else if (args[0] == "--port")
    {
        port = int.Parse(args[1]);
        args = args[2..];
    }
    else
    {
        break;
    }
}

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

    Console.WriteLine("Usage : Nidikwa.CLI.exe [--host <host>] [--port <port>] <operation name> [<operation parameters>]");
    Console.WriteLine();
}

await calledOperation.ExecuteAsync(host, port, args.Skip(1).ToArray());
