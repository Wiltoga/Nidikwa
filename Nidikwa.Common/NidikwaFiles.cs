namespace Nidikwa.Common;

public static class NidikwaFiles
{
    private const string WiltogaFolderName = "Wiltoga";
    private const string NidikwaFolderName = "Nidikwa";
    private const string QueueFolderName = "Queued";
    public static string QueueFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), WiltogaFolderName, NidikwaFolderName, QueueFolderName);

    public static void EnsureQueueFolderExists()
    {
        if (!Directory.Exists(QueueFolder))
        {
            Directory.CreateDirectory(QueueFolder);
        }
    }
}
