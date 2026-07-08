using SmartTyping.Application.Snippets;
using SmartTyping.Application.Templates;
using SmartTyping.Tests.Fakes;
using Xunit;

namespace SmartTyping.Tests;

public sealed class SnippetExpansionServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 7, 8, 14, 30, 0);

    private static (SnippetExpansionService service, FakeSnippetRepository repo) CreateService(string clipboard = "")
    {
        var repo = new FakeSnippetRepository();
        var clock = new FakeDateTimeProvider(FixedNow, FixedNow.ToUniversalTime());
        var templateEngine = new TemplateEngine(clock, new FakeClipboardService(clipboard));
        var service = new SnippetExpansionService(repo, templateEngine, clock);
        return (service, repo);
    }

    [Fact] // SE-1
    public async Task TryExpand_EnabledSnippet_ReturnsRenderedContent()
    {
        var (service, repo) = CreateService();
        repo.Seed("/sig", "Best regards");

        var result = await service.TryExpandAsync("/sig");

        Assert.True(result.Matched);
        Assert.Equal("Best regards", result.ExpandedText);
    }

    [Fact] // SE-1 (template)
    public async Task TryExpand_RendersTemplateVariables()
    {
        var (service, repo) = CreateService();
        repo.Seed("/date", "Today is {date}");

        var result = await service.TryExpandAsync("/date");

        Assert.True(result.Matched);
        Assert.Equal($"Today is {FixedNow.ToShortDateString()}", result.ExpandedText);
    }

    [Fact] // SE-2
    public async Task TryExpand_DisabledSnippet_DoesNotExpand()
    {
        var (service, repo) = CreateService();
        repo.Seed("/off", "should not expand", enabled: false);

        var result = await service.TryExpandAsync("/off");

        Assert.False(result.Matched);
        Assert.Equal(0, repo.RegisterUsageCallCount);
    }

    [Fact] // SE-3
    public async Task TryExpand_UnknownTrigger_ReturnsMiss()
    {
        var (service, _) = CreateService();

        var result = await service.TryExpandAsync("/nope");

        Assert.False(result.Matched);
    }

    [Fact] // SE-4
    public async Task TryExpand_TriggerMatchIsCaseInsensitive()
    {
        var (service, repo) = CreateService();
        repo.Seed("/sig", "signature");

        var result = await service.TryExpandAsync("/SIG");

        Assert.True(result.Matched);
        Assert.Equal("signature", result.ExpandedText);
    }

    [Fact] // SE-5
    public async Task TryExpand_DoesNotRecordUsage_UntilCallerConfirms()
    {
        // Usage is recorded by the caller (after a successful inject), not by TryExpandAsync, so a
        // failed paste can't inflate the stats.
        var (service, repo) = CreateService();
        var snippet = repo.Seed("/sig", "signature");

        var result = await service.TryExpandAsync("/sig");

        Assert.True(result.Matched);
        Assert.Equal(0, repo.RegisterUsageCallCount);

        await service.RegisterUsageAsync(result.SnippetId!.Value);

        Assert.Equal(1, repo.RegisterUsageCallCount);
        Assert.Equal(1, snippet.UsageCount);
    }

    [Fact]
    public async Task TryExpand_EmptyTrigger_ReturnsMiss()
    {
        var (service, _) = CreateService();
        var result = await service.TryExpandAsync("   ");
        Assert.False(result.Matched);
    }

    [Fact]
    public async Task TryExpand_InputPromptCancelled_MissesAndDoesNotRecordUsage()
    {
        var repo = new FakeSnippetRepository();
        repo.Seed("/email", "Hi {input:Name}");
        var clock = new FakeDateTimeProvider(FixedNow, FixedNow.ToUniversalTime());
        var cancelling = new FakePlaceholderPrompt(values: null);
        var templateEngine = new TemplateEngine(clock, new FakeClipboardService(), cancelling);
        var service = new SnippetExpansionService(repo, templateEngine, clock);

        var result = await service.TryExpandAsync("/email");

        Assert.False(result.Matched);
        Assert.Equal(0, repo.RegisterUsageCallCount);
    }
}
