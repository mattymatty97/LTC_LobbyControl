using System;
using System.Diagnostics;

namespace LobbyControl.Utils;

/// <summary>
/// A stopwatch-backed timer utility for tracking elapsed time against a specified timeout duration.
/// </summary>
public class FreeRunningTimer
{
    /// <summary>
    /// Gets a value indicating whether the timer is currently running.
    /// </summary>
    public bool Enabled => _stopwatch.IsRunning;

    /// <summary>
    /// Gets a value indicating whether the elapsed duration has exceeded the configured timeout.
    /// </summary>
    public bool TimedOut => _stopwatch.Elapsed > _timeout;

    /// <summary>
    /// Gets the total elapsed time measured by the timer since its last start or restart.
    /// </summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>
    /// Gets the remaining time before timeout expiration, or <see cref="TimeSpan.Zero"/> if the timer has timed out.
    /// </summary>
    public TimeSpan Remaining => TimedOut ? TimeSpan.Zero : _timeout - _stopwatch.Elapsed;

    /// <summary>
    /// Starts or restarts the timer with the specified timeout duration.
    /// </summary>
    /// <param name="timeout">The duration after which the timer will be considered timed out.</param>
    public void Start(TimeSpan timeout) {
        _timeout = timeout;
        _stopwatch.Restart();
    }

    /// <summary>
    /// Stops the running timer.
    /// </summary>
    public void Stop() => _stopwatch.Stop();

    /// <summary>
    /// Restarts the timer if it is currently active and has not timed out.
    /// </summary>
    /// <returns><see langword="true"/> if the timer was running and successfully restarted; otherwise, <see langword="false"/>.</returns>
    public bool Restart()
    {
        if (!_stopwatch.IsRunning || TimedOut)
            return false;

        _stopwatch.Restart();
        return true;
    }

    private readonly Stopwatch _stopwatch = new Stopwatch();
    private TimeSpan _timeout = TimeSpan.Zero;
}
