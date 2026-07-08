using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartTyping.Application.Abstractions;
using SmartTyping.Infrastructure.Input;
using SmartTyping.Infrastructure.Logging;
using SmartTyping.Infrastructure.Persistence;
using SmartTyping.Infrastructure.Persistence.Repositories;
using SmartTyping.Infrastructure.Platform;
using SmartTyping.Infrastructure.Time;

namespace SmartTyping.Infrastructure;

/// <summary>
/// Registers Infrastructure implementations of the Application ports: SQLite/Dapper repositories,
/// Windows input services, clock, and logging.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? databaseFilePath = null)
    {
        // Dapper: map DateTime columns back to UTC kind (one-time, process-wide).
        DapperConfiguration.Register();

        // Logging (Serilog → Microsoft.Extensions.Logging).
        services.AddSingleton(LoggingSetup.CreateLoggerFactory());
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        // Persistence.
        services.AddSingleton<ISqlConnectionFactory>(_ => new SqliteConnectionFactory(databaseFilePath));
        services.AddSingleton<DatabaseInitializer>();
        services.AddTransient<ISnippetRepository, SnippetRepository>();
        services.AddTransient<ICategoryRepository, CategoryRepository>();
        services.AddTransient<ISettingsRepository, SettingsRepository>();

        // Time.
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Windows input / clipboard / injection.
        // One WindowsClipboardService instance backs both the text port and the full-clipboard backup.
        services.AddSingleton<WindowsClipboardService>();
        services.AddSingleton<IClipboardService>(sp => sp.GetRequiredService<WindowsClipboardService>());
        services.AddSingleton<IClipboardBackup>(sp => sp.GetRequiredService<WindowsClipboardService>());
        services.AddSingleton<ITextInjector, WindowsTextInjector>();
        services.AddSingleton<ISelectionService, WindowsSelectionService>();
        services.AddSingleton<ISecureInputDetector, WindowsSecureInputDetector>();
        services.AddSingleton<IForegroundWindowService, WindowsForegroundWindowService>();
        services.AddSingleton<IKeyboardHook, WindowsKeyboardHook>();

        // OS integration.
        services.AddSingleton<IStartupService, WindowsStartupService>();

        return services;
    }
}
