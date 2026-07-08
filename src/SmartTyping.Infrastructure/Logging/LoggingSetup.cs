using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using SmartTyping.Infrastructure.Persistence;

namespace SmartTyping.Infrastructure.Logging;

/// <summary>
/// Configures Serilog with a rolling file sink under <c>%LOCALAPPDATA%\SmartTyping\logs</c>
/// and exposes it as a Microsoft.Extensions.Logging <see cref="ILoggerFactory"/>.
/// </summary>
public static class LoggingSetup
{
    public static ILoggerFactory CreateLoggerFactory()
    {
        var logPath = Path.Combine(AppPaths.LogDirectory, "smarttyping-.log");

        var serilog = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        return new SerilogLoggerFactory(serilog, dispose: true);
    }
}
