using SmartTyping.Application.Update;
using Xunit;

namespace SmartTyping.Tests;

public sealed class UpdateComparerTests
{
    [Theory]
    [InlineData("0.1.0", "v0.2.0", true)]
    [InlineData("0.1.0", "0.1.1", true)]
    [InlineData("0.1.0.0", "v0.2", true)]
    [InlineData("1.0.0", "v2.0.0-beta", true)]   // pre-release suffix ignored
    [InlineData("0.2.0", "v0.2.0", false)]        // same version
    [InlineData("0.2.0", "v0.1.9", false)]        // older
    [InlineData("0.1.0.0", "v0.1", false)]        // equal after normalization
    public void IsNewer(string current, string latest, bool expected)
    {
        Assert.Equal(expected, UpdateComparer.IsNewer(current, latest));
    }

    [Theory]
    [InlineData(null, "v1.0.0")]
    [InlineData("0.1.0", null)]
    [InlineData("0.1.0", "garbage")]
    public void IsNewer_InvalidInput_ReturnsFalse(string? current, string? latest)
    {
        Assert.False(UpdateComparer.IsNewer(current, latest));
    }
}
