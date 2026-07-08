using Microsoft.Extensions.DependencyInjection;
using SmartTyping.Application.Abstractions;
using SmartTyping.Application.Language;
using SmartTyping.Application.Settings;
using SmartTyping.Application.Snippets;
using SmartTyping.Application.Templates;

namespace SmartTyping.Application;

/// <summary>
/// Registers Application-layer services (pure logic + use cases). Infrastructure ports
/// (repositories, clipboard, hooks, injection, clock) are registered by the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Pure implementations that live in Application.
        services.AddSingleton<IKeyboardLayoutConverter, KeyboardLayoutConverter>();
        services.AddTransient<ITemplateEngine, TemplateEngine>();

        // Use-case services.
        services.AddTransient<SnippetExpansionService>();
        services.AddTransient<LanguageConversionService>();
        services.AddTransient<SettingsService>();
        services.AddTransient<ISnippetImportExportService, SnippetImportExportService>();

        return services;
    }
}
