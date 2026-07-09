using SmartTyping.Application.Settings;
using SmartTyping.Domain.Enums;
using SmartTyping.Tests.Fakes;
using Xunit;

namespace SmartTyping.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task Defaults_ToTrue_WhenSettingAbsent()
    {
        var service = new SettingsService(new FakeSettingsRepository());

        Assert.True(await service.IsSnippetExpansionEnabledAsync());
        Assert.True(await service.IsLanguageCorrectionEnabledAsync());

        // Notifications are on until the user turns them off.
        Assert.True(await service.IsNotificationsEnabledAsync());
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("yes", true)]
    [InlineData("no", false)]
    [InlineData("TRUE", true)]
    [InlineData(" False ", false)]
    public async Task ParsesStoredBooleanValues(string stored, bool expected)
    {
        var repo = new FakeSettingsRepository((SettingKeys.SnippetExpansionEnabled, stored));
        var service = new SettingsService(repo);

        Assert.Equal(expected, await service.IsSnippetExpansionEnabledAsync());
    }

    [Fact]
    public async Task Set_PersistsCanonicalValue()
    {
        var repo = new FakeSettingsRepository();
        var service = new SettingsService(repo);

        await service.SetLanguageCorrectionEnabledAsync(false);
        Assert.Equal("false", await repo.GetAsync(SettingKeys.LanguageCorrectionEnabled));

        await service.SetLanguageCorrectionEnabledAsync(true);
        Assert.Equal("true", await repo.GetAsync(SettingKeys.LanguageCorrectionEnabled));
    }

    [Fact]
    public async Task Unparseable_FallsBackToDefault()
    {
        var repo = new FakeSettingsRepository((SettingKeys.LanguageCorrectionEnabled, "garbage"));
        var service = new SettingsService(repo);

        // Default for language correction is true.
        Assert.True(await service.IsLanguageCorrectionEnabledAsync());
    }
}
