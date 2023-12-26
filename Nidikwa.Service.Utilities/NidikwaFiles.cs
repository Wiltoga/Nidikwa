namespace Nidikwa.Service.Utilities;

public static class NidikwaFiles
{
    private const string NidikwaFolderName = "Nidikwa";
    private const string QueueFolderName = "Queued";
    public static string QueueFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), NidikwaFolderName, QueueFolderName);

    public static void EnsureQueueFolderExists()
    {
        if (!Directory.Exists(QueueFolder))
        {
            Directory.CreateDirectory(QueueFolder);
        }
    }
}
