namespace SmartTyping.Infrastructure.Persistence;

/// <summary>
/// Resolves per-user file locations under <c>%LOCALAPPDATA%\SmartTyping</c>.
/// The directory is created on demand.
/// </summary>
public static class AppPaths
{
    public static string DataDirectory
    {
        get
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(root, "SmartTyping");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string DatabaseFile => Path.Combine(DataDirectory, "smarttyping.db");

    public static string LogDirectory
    {
        get
        {
            var dir = Path.Combine(DataDirectory, "logs");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
