namespace SmartTyping.Infrastructure.Input;

/// <summary>
/// Small polling helper to wait for a clipboard condition instead of relying on fixed sleeps.
/// Makes clipboard-transport injection/capture more robust on slow or remote applications.
/// </summary>
internal static class ClipboardWait
{
    private const int PollIntervalMs = 15;

    /// <summary>
    /// Polls <paramref name="condition"/> until it returns true or <paramref name="timeoutMs"/>
    /// elapses. Returns whether the condition was met.
    /// </summary>
    public static async Task<bool> UntilAsync(Func<Task<bool>> condition, int timeoutMs)
    {
        var waited = 0;
        while (waited < timeoutMs)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(PollIntervalMs);
            waited += PollIntervalMs;
        }

        return await condition();
    }
}
