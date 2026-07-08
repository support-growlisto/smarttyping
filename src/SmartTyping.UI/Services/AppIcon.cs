using System.IO;
using System.Windows.Media.Imaging;

namespace SmartTyping.UI.Services;

/// <summary>Loads the application icon (shipped as <c>assets/app.ico</c> next to the exe) for window chrome.</summary>
public static class AppIcon
{
    /// <summary>Returns the app icon as an image source, or null if it cannot be loaded.</summary>
    public static BitmapFrame? TryLoad()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "assets", "app.ico");
            if (!File.Exists(path))
            {
                return null;
            }

            return BitmapFrame.Create(
                new Uri(path),
                BitmapCreateOptions.None,
                BitmapCacheOption.OnLoad);
        }
        catch
        {
            return null;
        }
    }
}
