using System.Threading;

namespace SmartTyping.UI.Services;

/// <summary>
/// Ensures only one instance of the app runs at a time (a second launch would install a duplicate
/// keyboard hook, tray icon, and DB writer). Uses a named <see cref="Mutex"/> to detect an existing
/// instance and a named <see cref="EventWaitHandle"/> so a second launch can ask the first to surface
/// its window instead of starting again.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _showSignal;
    private Thread? _listener;
    private volatile bool _running = true;

    public SingleInstanceGuard(string name)
    {
        _mutex = new Mutex(initiallyOwned: true, $"{name}.Mutex", out var createdNew);
        IsFirstInstance = createdNew;
        _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, $"{name}.Show");
    }

    /// <summary>True if this process is the first/only instance.</summary>
    public bool IsFirstInstance { get; }

    /// <summary>Asks the already-running instance to show its window.</summary>
    public void SignalExistingInstance() => _showSignal.Set();

    /// <summary>Starts a background listener that invokes <paramref name="onShowRequested"/> when signalled.</summary>
    public void StartListening(Action onShowRequested)
    {
        _listener = new Thread(() =>
        {
            while (_running)
            {
                _showSignal.WaitOne();
                if (_running)
                {
                    onShowRequested();
                }
            }
        })
        {
            IsBackground = true,
            Name = "SingleInstanceListener"
        };
        _listener.Start();
    }

    public void Dispose()
    {
        _running = false;
        _showSignal.Set(); // wake the listener so it can exit
        try
        {
            if (IsFirstInstance)
            {
                _mutex.ReleaseMutex();
            }
        }
        catch (ApplicationException)
        {
            // Mutex not owned by this thread — safe to ignore on shutdown.
        }

        _mutex.Dispose();
        _showSignal.Dispose();
    }
}
