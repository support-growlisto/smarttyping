using SmartTyping.Application.Settings;
using Xunit;

namespace SmartTyping.Tests;

public sealed class AppBlocklistTests
{
    private static readonly AppBlocklist List = new(["cmd", "mstsc", "KeePass"]);

    [Theory]
    [InlineData("cmd")]
    [InlineData("cmd.exe")]
    [InlineData("CMD.EXE")]
    [InlineData(@"C:\Windows\System32\cmd.exe")]
    [InlineData("C:/Windows/System32/cmd.exe")]
    public void MatchesRegardlessOfPathCaseAndExtension(string processName) =>
        Assert.True(List.IsBlocked(processName));

    [Theory]
    [InlineData("notepad")]
    [InlineData("cmder")]      // not a prefix match
    [InlineData("xcmd")]
    public void DoesNotMatchOtherApps(string processName) => Assert.False(List.IsBlocked(processName));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UnknownForegroundProcess_IsNotBlocked(string? processName)
    {
        // If we cannot identify the app we must keep working, not silently disable ourselves.
        Assert.False(List.IsBlocked(processName));
    }

    [Fact]
    public void EntriesAreNormalizedAndDeduplicated()
    {
        var list = new AppBlocklist([" cmd.exe ", "CMD", @"C:\x\cmd.exe", "putty"]);

        Assert.Equal(new[] { "cmd", "putty" }, list.Entries);
    }

    [Fact]
    public void Parse_EmptyOrMissing_FallsBackToDefaults()
    {
        Assert.Equal(new AppBlocklist(AppBlocklist.Defaults).Entries, AppBlocklist.Parse(null).Entries);
        Assert.Equal(new AppBlocklist(AppBlocklist.Defaults).Entries, AppBlocklist.Parse("  ").Entries);

        Assert.True(AppBlocklist.Parse(null).IsBlocked("mstsc.exe"));
    }

    [Fact]
    public void Parse_AcceptsCommasSemicolonsAndNewlines()
    {
        var list = AppBlocklist.Parse("cmd, putty; notepad\nmstsc");

        Assert.True(list.IsBlocked("putty"));
        Assert.True(list.IsBlocked("notepad"));
        Assert.True(list.IsBlocked("mstsc"));
    }

    [Fact]
    public void RoundTripsThroughItsPersistedForm()
    {
        var original = AppBlocklist.Parse("cmd, mstsc, keepass");

        Assert.Equal(original.Entries, AppBlocklist.Parse(original.ToString()).Entries);
    }

    [Fact]
    public void Defaults_BlockTerminalsRemoteSessionsAndPasswordManagers()
    {
        var list = new AppBlocklist(AppBlocklist.Defaults);

        Assert.True(list.IsBlocked("cmd.exe"));
        Assert.True(list.IsBlocked("mstsc.exe"));      // remote desktop
        Assert.True(list.IsBlocked("keepassxc.exe"));
        Assert.False(list.IsBlocked("notepad.exe"));   // ordinary editors keep working
        Assert.False(list.IsBlocked("chrome.exe"));
    }
}
