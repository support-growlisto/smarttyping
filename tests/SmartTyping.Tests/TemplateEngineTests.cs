using SmartTyping.Application.Templates;
using SmartTyping.Tests.Fakes;
using Xunit;

namespace SmartTyping.Tests;

public sealed class TemplateEngineTests
{
    private static readonly DateTime FixedNow = new(2026, 7, 8, 14, 30, 0);

    private static TemplateEngine CreateEngine(string clipboard = "")
    {
        var clock = new FakeDateTimeProvider(FixedNow, FixedNow.ToUniversalTime());
        return new TemplateEngine(clock, new FakeClipboardService(clipboard));
    }

    private static TemplateEngine CreateEngineWithPrompt(FakePlaceholderPrompt prompt)
    {
        var clock = new FakeDateTimeProvider(FixedNow, FixedNow.ToUniversalTime());
        return new TemplateEngine(clock, new FakeClipboardService(), prompt);
    }

    [Fact] // TE-1
    public async Task Render_DateToken_ReplacedWithShortDate()
    {
        var result = await CreateEngine().RenderAsync("Today is {date}");
        Assert.Equal($"Today is {FixedNow.ToShortDateString()}", result.Text);
    }

    [Fact] // TE-2
    public async Task Render_TimeToken_ReplacedWithShortTime()
    {
        var result = await CreateEngine().RenderAsync("Now: {time}");
        Assert.Equal($"Now: {FixedNow.ToShortTimeString()}", result.Text);
    }

    [Fact] // TE-3
    public async Task Render_ClipboardToken_ReplacedWithClipboardText()
    {
        var result = await CreateEngine("copied text").RenderAsync("Paste: {clipboard}");
        Assert.Equal("Paste: copied text", result.Text);
    }

    [Fact] // TE-4
    public async Task Render_UnknownToken_LeftUnchanged()
    {
        var result = await CreateEngine().RenderAsync("Value: {unknown}");
        Assert.Equal("Value: {unknown}", result.Text);
    }

    [Fact] // TE-5
    public async Task Render_TokenIsCaseInsensitive()
    {
        var result = await CreateEngine().RenderAsync("D: {Date}");
        Assert.Equal($"D: {FixedNow.ToShortDateString()}", result.Text);
    }

    [Fact]
    public async Task Render_ContentWithoutTokens_ReturnedVerbatim()
    {
        const string content = "no variables here";
        var result = await CreateEngine().RenderAsync(content);
        Assert.Equal(content, result.Text);
        Assert.Null(result.CursorOffset);
    }

    [Fact]
    public async Task Render_UnclosedBrace_ReturnedVerbatim()
    {
        const string content = "dangling {date";
        var result = await CreateEngine().RenderAsync(content);
        Assert.Equal(content, result.Text);
    }

    [Fact]
    public async Task Render_DateWithCustomFormat()
    {
        var result = await CreateEngine().RenderAsync("{date:yyyy-MM-dd}");
        Assert.Equal("2026-07-08", result.Text);
    }

    [Fact]
    public async Task Render_DateWithPositiveOffset()
    {
        var result = await CreateEngine().RenderAsync("{date+7:yyyy-MM-dd}");
        Assert.Equal("2026-07-15", result.Text);
    }

    [Fact]
    public async Task Render_DateWithNegativeOffset()
    {
        var result = await CreateEngine().RenderAsync("{date-1:yyyy-MM-dd}");
        Assert.Equal("2026-07-07", result.Text);
    }

    [Fact]
    public async Task Render_TimeWithCustomFormat()
    {
        var result = await CreateEngine().RenderAsync("{time:HH:mm}");
        Assert.Equal("14:30", result.Text);
    }

    [Fact]
    public async Task Render_InvalidDateOffset_LeftVerbatim()
    {
        var result = await CreateEngine().RenderAsync("{date+abc}");
        Assert.Equal("{date+abc}", result.Text);
    }

    [Fact]
    public async Task Render_CursorMarker_RecordsOffsetAndProducesNoText()
    {
        var result = await CreateEngine().RenderAsync("Hi {cursor}!");
        Assert.Equal("Hi !", result.Text);
        Assert.Equal(3, result.CursorOffset);
    }

    [Fact]
    public async Task Render_FirstCursorMarkerWins()
    {
        var result = await CreateEngine().RenderAsync("a{cursor}b{cursor}c");
        Assert.Equal("abc", result.Text);
        Assert.Equal(1, result.CursorOffset);
    }

    [Fact]
    public async Task Render_InputPlaceholder_SubstitutesPromptedValue()
    {
        var prompt = new FakePlaceholderPrompt(new Dictionary<string, string> { ["Name"] = "Alice" });
        var result = await CreateEngineWithPrompt(prompt).RenderAsync("Hi {input:Name}!");

        Assert.Equal("Hi Alice!", result.Text);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public async Task Render_SameInputLabel_PromptedOnce()
    {
        var prompt = new FakePlaceholderPrompt(new Dictionary<string, string> { ["Name"] = "Bob" });
        var result = await CreateEngineWithPrompt(prompt).RenderAsync("{input:Name} + {input:Name}");

        Assert.Equal("Bob + Bob", result.Text);
        Assert.NotNull(prompt.LastLabels);
        Assert.Single(prompt.LastLabels!);
    }

    [Fact]
    public async Task Render_MultipleInputs_CollectedInOrder()
    {
        var prompt = new FakePlaceholderPrompt(new Dictionary<string, string> { ["First"] = "a", ["Second"] = "b" });
        var result = await CreateEngineWithPrompt(prompt).RenderAsync("{input:First}-{input:Second}");

        Assert.Equal("a-b", result.Text);
        Assert.Equal(new[] { "First", "Second" }, prompt.LastLabels);
    }

    [Fact]
    public async Task Render_InputCancelled_ReturnsCancelled()
    {
        var prompt = new FakePlaceholderPrompt(values: null); // simulate cancel
        var result = await CreateEngineWithPrompt(prompt).RenderAsync("Hi {input:Name}!");

        Assert.True(result.Cancelled);
    }

    [Fact]
    public async Task Render_InputWithNoPrompt_LeftEmpty()
    {
        // No prompt provider (e.g. tests / headless) → input tokens resolve to empty, never cancel.
        var result = await CreateEngine().RenderAsync("Hi {input:Name}!");

        Assert.Equal("Hi !", result.Text);
        Assert.False(result.Cancelled);
    }
}
