using SmartTyping.Application.Snippets;
using SmartTyping.Tests.Fakes;
using Xunit;

namespace SmartTyping.Tests;

public sealed class SnippetImportExportServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 7, 8, 14, 30, 0);

    private static (SnippetImportExportService service, FakeSnippetRepository snippets, FakeCategoryRepository categories) Create()
    {
        var snippets = new FakeSnippetRepository();
        var categories = new FakeCategoryRepository();
        var clock = new FakeDateTimeProvider(FixedNow, FixedNow.ToUniversalTime());
        return (new SnippetImportExportService(snippets, categories, clock), snippets, categories);
    }

    [Fact]
    public async Task Export_ContainsSeededTriggers()
    {
        var (service, snippets, _) = Create();
        snippets.Seed("/sig", "Best regards");

        var json = await service.ExportAsync();

        Assert.Contains("/sig", json);
        Assert.Contains("Best regards", json);
    }

    [Fact]
    public async Task Import_AddsNewSnippets()
    {
        var (service, snippets, _) = Create();
        const string json = """
            { "version": 1, "snippets": [ { "trigger": "/new", "content": "hello", "isEnabled": true } ] }
            """;

        var summary = await service.ImportAsync(json, ImportMode.Skip);

        Assert.Equal(1, summary.Added);
        Assert.NotNull(await snippets.FindByTriggerAsync("/new"));
    }

    [Fact]
    public async Task Import_SkipMode_KeepsExistingContent()
    {
        var (service, snippets, _) = Create();
        snippets.Seed("/dup", "original");
        const string json = """
            { "snippets": [ { "trigger": "/dup", "content": "changed" } ] }
            """;

        var summary = await service.ImportAsync(json, ImportMode.Skip);

        Assert.Equal(1, summary.Skipped);
        Assert.Equal("original", (await snippets.FindByTriggerAsync("/dup"))!.Content);
    }

    [Fact]
    public async Task Import_OverwriteMode_UpdatesExistingContent()
    {
        var (service, snippets, _) = Create();
        snippets.Seed("/dup", "original");
        const string json = """
            { "snippets": [ { "trigger": "/dup", "content": "changed" } ] }
            """;

        var summary = await service.ImportAsync(json, ImportMode.Overwrite);

        Assert.Equal(1, summary.Updated);
        Assert.Equal("changed", (await snippets.FindByTriggerAsync("/dup"))!.Content);
    }

    [Fact]
    public async Task Import_CreatesMissingCategory()
    {
        var (service, _, categories) = Create();
        const string json = """
            { "snippets": [ { "trigger": "/w", "content": "x", "category": "Work" } ] }
            """;

        await service.ImportAsync(json, ImportMode.Skip);

        Assert.Contains(categories.Items, c => c.Name == "Work");
    }

    [Fact]
    public async Task Import_SkipsEntriesMissingTriggerOrContent()
    {
        var (service, _, _) = Create();
        const string json = """
            { "snippets": [ { "trigger": "", "content": "x" }, { "trigger": "/ok", "content": "" } ] }
            """;

        var summary = await service.ImportAsync(json, ImportMode.Skip);

        Assert.Equal(2, summary.Skipped);
        Assert.Equal(0, summary.Added);
    }

    [Fact]
    public async Task ExportThenImport_RoundTripsSnippets()
    {
        var (exportService, sourceSnippets, _) = Create();
        sourceSnippets.Seed("/a", "alpha");
        sourceSnippets.Seed("/b", "beta");
        var json = await exportService.ExportAsync();

        var (importService, targetSnippets, _) = Create();
        var summary = await importService.ImportAsync(json, ImportMode.Skip);

        Assert.Equal(2, summary.Added);
        Assert.Equal("alpha", (await targetSnippets.FindByTriggerAsync("/a"))!.Content);
        Assert.Equal("beta", (await targetSnippets.FindByTriggerAsync("/b"))!.Content);
    }
}
