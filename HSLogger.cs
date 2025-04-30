namespace HSLocationManager;

using System;
using System.IO;
using System.Linq;
using Foundation;
using UIKit;

public class HSLogger : NSObject, IUIDocumentInteractionControllerDelegate
{
    #region Properties

    public ulong MaxFileSize { get; set; } = 2048;
    public int MaxFileCount { get; set; } = 8;
    public string Name { get; set; } = "logfile";
    public bool PrintToConsole { get; set; } = true;

    private static readonly Lazy<HSLogger> _instance = new(() => new HSLogger());
    public static HSLogger Logger => _instance.Value;

    private string _directory = DefaultDirectory();
    private readonly NSDateFormatter _dateFormatter;

    public string Directory
    {
        get => _directory;
        set
        {
            _directory = Path.GetFullPath(value);
            EnsureDirectoryExists(_directory);
        }
    }

    public string CurrentPath => Path.Combine(Directory, GetLogName(0));

    #endregion

    #region Constructor

    private HSLogger()
    {
#pragma warning disable CA1416 // Validate platform compatibility
        _dateFormatter = new NSDateFormatter
        {
            TimeStyle = NSDateFormatterStyle.Medium,
            DateStyle = NSDateFormatterStyle.Medium
        };
#pragma warning restore CA1416 // Validate platform compatibility
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Exports the current log file using iOS document interaction controller
    /// </summary>
    public void ExportLogFile()
    {
#pragma warning disable CA1416 // Validate platform compatibility
        var url = NSUrl.FromFilename(CurrentPath);
        var docController = new UIDocumentInteractionController
        {
            Url = url,
            Uti = "public.comma-separated-values-text",
            Delegate = this
        };
        docController.PresentPreview(true);
#pragma warning restore CA1416 // Validate platform compatibility
    }

    /// <summary>
    /// Writes a log entry to the current log file
    /// </summary>
    /// <param name="text">The log message to write</param>
    public void Write(string text)
    {
        try
        {
            EnsureLogFileExists();

            using var fileStream = new FileStream(CurrentPath, FileMode.Append, FileAccess.Write);
            using var writer = new StreamWriter(fileStream);

#pragma warning disable CA1416 // Validate platform compatibility
            var dateStr = _dateFormatter.ToString(NSDate.Now);
#pragma warning restore CA1416 // Validate platform compatibility
            var logEntry = $"[{dateStr}]: {text}\n";

            writer.Write(logEntry);

            if (PrintToConsole)
            {
                Console.Write(logEntry);
            }

            ManageLogFiles();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to log: {ex.Message}");
        }
    }

    #endregion

    #region IUIDocumentInteractionControllerDelegate Implementation

    [Export("documentInteractionControllerViewControllerForPreview:")]
    public UIViewController ViewControllerForPreview(UIDocumentInteractionController controller)
    {
        // Get the current key window's root view controller
#pragma warning disable CA1422 // Valider la compatibilité de la plateforme
        var window = UIApplication.SharedApplication.KeyWindow
            ?? UIApplication.SharedApplication.Windows.FirstOrDefault(w => w.IsKeyWindow);
#pragma warning restore CA1422 // Valider la compatibilité de la plateforme

        return window?.RootViewController;
    }
    #endregion

    #region Private Methods

    private void EnsureLogFileExists()
    {
        if (!File.Exists(CurrentPath))
        {
            try
            {
                File.WriteAllText(CurrentPath, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create log file: {ex.Message}");
            }
        }
    }

    private void ManageLogFiles()
    {
        var currentSize = GetFileSize(CurrentPath);
        var maxSize = MaxFileSize * 1024;

        if (currentSize > 0 && currentSize >= (long)maxSize && maxSize > 0 && MaxFileCount > 0)
        {
            RotateLogFiles();
        }
    }

    private void RotateLogFiles()
    {
        // Start rotation with the oldest file first
        var deletePath = Path.Combine(Directory, GetLogName(MaxFileCount));
        try
        {
            if (File.Exists(deletePath))
            {
                File.Delete(deletePath);
            }

            // Rotate files from highest index to lowest
            for (int i = MaxFileCount - 1; i >= 0; i--)
            {
                var currentFile = Path.Combine(Directory, GetLogName(i));
                var nextFile = Path.Combine(Directory, GetLogName(i + 1));

                if (File.Exists(currentFile))
                {
                    File.Move(currentFile, nextFile, true);
                }
            }

            // Create a new empty log file
            File.WriteAllText(CurrentPath, string.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rotating log files: {ex.Message}");
        }
    }

    private static long GetFileSize(string path)
    {
        return File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    private string GetLogName(int num) => $"{Name}-{num}.log";

    private static void EnsureDirectoryExists(string path)
    {
        if (!System.IO.Directory.Exists(path))
        {
            try
            {
                System.IO.Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't create directory at {path}: {ex.Message}");
            }
        }
    }

    private static string DefaultDirectory()
    {
#pragma warning disable CA1416 // Validate platform compatibility
        var paths = NSSearchPath.GetDirectories(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User, true);
#pragma warning restore CA1416 // Validate platform compatibility
        var logPath = Path.Combine(paths[0], "Logs");

        EnsureDirectoryExists(logPath);

        return logPath;
    }

    #endregion
}

/// <summary>
/// Static helper class for easier logging
/// </summary>
public static class LoggerHelper
{
    /// <summary>
    /// Writes a log entry using the singleton logger instance
    /// </summary>
    /// <param name="text">The log message to write</param>
    public static void Log(string text)
    {
        HSLogger.Logger.Write(text);
    }
}