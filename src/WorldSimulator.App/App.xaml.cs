using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WorldSimulator.App;

public partial class App : Application
{
    private static readonly string CrashLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "startup-crash.log");

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
    }

    private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        WriteCrashLog("AppDomain", exception);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrashLog("Dispatcher", e.Exception);

        MessageBox.Show(
            "Приложение столкнулось с ошибкой и будет закрыто.\nПодробности записаны в logs/startup-crash.log",
            "Критическая ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
        Current.Shutdown();
    }

    private static void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteCrashLog("TaskScheduler", e.Exception);
        e.SetObserved();
    }

    private static void WriteCrashLog(string source, Exception? exception)
    {
        try
        {
            var logDirectory = Path.GetDirectoryName(CrashLogPath);
            if (!string.IsNullOrEmpty(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            var payload = new StringBuilder()
                .AppendLine($"utc_timestamp: {DateTime.UtcNow:O}")
                .AppendLine($"source: {source}")
                .AppendLine($"exception_type: {exception?.GetType().FullName ?? "Unknown"}")
                .AppendLine($"exception_message: {exception?.Message ?? "No message"}")
                .AppendLine("exception:")
                .AppendLine(exception?.ToString() ?? "<null>")
                .AppendLine(new string('-', 80))
                .ToString();

            File.AppendAllText(CrashLogPath, payload, Encoding.UTF8);
        }
        catch
        {
            // Intentionally swallowed: logging must never crash the app.
        }
    }
}
