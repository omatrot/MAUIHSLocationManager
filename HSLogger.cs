namespace HSLocationManager;

using System;
using System.IO;
using Foundation;
using UIKit;

public class HSLogger : NSObject, IUIDocumentInteractionControllerDelegate
{
    public ulong MaxFileSize { get; set; } = 2048;
    public int MaxFileCount { get; set; } = 8;
    public string Name { get; set; } = "logfile";
    public bool PrintToConsole { get; set; } = true;

    private static readonly Lazy<HSLogger> _instance = new(() => new HSLogger());
    public static HSLogger Logger => _instance.Value;

    private string _directory = DefaultDirectory();

    public string Directory
    {
        get => _directory;
        set
        {
            _directory = Path.GetFullPath(value);
            if (!System.IO.Directory.Exists(_directory))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(_directory);
                }
                catch
                {
                    Console.WriteLine($"Couldn't create directory at {_directory}");
                }
            }
        }
    }

    public string CurrentPath => Path.Combine(Directory, LogName(0));

    private NSDateFormatter DateFormatter
    {
        get
        {
            var formatter = new NSDateFormatter
            {
                TimeStyle = NSDateFormatterStyle.Medium,
                DateStyle = NSDateFormatterStyle.Medium
            };
            return formatter;
        }
    }

    public void ExportLogFile()
    {
        var url = NSUrl.FromFilename(CurrentPath);
        var docController = new UIDocumentInteractionController
        {
            Url = url,
            Uti = "public.comma-separated-values-text",
            Delegate = this
        };
        docController.PresentPreview(true);
    }

    public UIViewController? DocumentInteractionControllerViewControllerForPreview(UIDocumentInteractionController controller)
    {
#pragma warning disable CA1416 // Valider la compatibilité de la plateforme
        var windowScene = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .FirstOrDefault();
#pragma warning restore CA1416 // Valider la compatibilité de la plateforme

#pragma warning disable CA1416 // Valider la compatibilité de la plateforme
        return windowScene?.Windows.FirstOrDefault(w => w.IsKeyWindow)?.RootViewController;
#pragma warning restore CA1416 // Valider la compatibilité de la plateforme
    }


    public void Write(string text)
    {
        var path = CurrentPath;
        if (!File.Exists(path))
        {
            try
            {
                File.WriteAllText(path, "");
            }
            catch { }
        }

        using var fileHandle = new FileStream(path, FileMode.Append, FileAccess.Write);
        var dateStr = DateFormatter.ToString(NSDate.Now);
        var writeText = $"[{dateStr}]: {text}\n";
        using var writer = new StreamWriter(fileHandle);
        writer.Write(writeText);

        if (PrintToConsole)
        {
            Console.Write(writeText);
        }

        Cleanup();
    }

    private void Cleanup()
    {
        var path = Path.Combine(Directory, LogName(0));
        var size = FileSize(path);
        var maxSize = MaxFileSize * 1024;

        if (size > 0 && size >= (long)maxSize && maxSize > 0 && MaxFileCount > 0)
        {
            Rename(0);
            var deletePath = Path.Combine(Directory, LogName(MaxFileCount));
            try
            {
                File.Delete(deletePath);
            }
            catch { }
        }
    }

    private static long FileSize(string path)
    {
        return File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    private void Rename(int index)
    {
        var path = Path.Combine(Directory, LogName(index));
        var newPath = Path.Combine(Directory, LogName(index + 1));

        if (File.Exists(newPath))
        {
            Rename(index + 1);
        }

        try
        {
            File.Move(path, newPath);
        }
        catch { }
    }

    private static string LogName(int num) => $"logfile-{num}.log";

    private static string DefaultDirectory()
    {
        string path = "";
        var fileManager = NSFileManager.DefaultManager;

        var paths = NSSearchPath.GetDirectories(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User, true);
        path = Path.Combine(paths[0], "Logs");

        if (!System.IO.Directory.Exists(path))
        {
            try
            {
                System.IO.Directory.CreateDirectory(path);
            }
            catch { }
        }

        return path;
    }
}

public static class LoggerHelper
{
    public static void Log(string text)
    {
        HSLogger.Logger.Write(text);
    }
}
