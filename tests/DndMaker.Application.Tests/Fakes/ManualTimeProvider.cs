namespace DndMaker.Application.Tests.Fakes;

/// <summary>Relógio que só anda quando o teste manda (<see cref="Advance"/>). Os timers disparam na thread do teste.</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        _timers.Add(timer);
        return timer;
    }

    public void Advance(TimeSpan amount)
    {
        _now += amount;
        foreach (var timer in _timers.ToList())
        {
            timer.FireIfDue(_now);
        }
    }

    private sealed class ManualTimer(ManualTimeProvider clock, TimerCallback callback, object? state) : ITimer
    {
        private DateTimeOffset? _dueAt;
        private bool _disposed;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (_disposed)
            {
                return false;
            }

            _dueAt = dueTime == Timeout.InfiniteTimeSpan ? null : clock._now + dueTime;
            return true;
        }

        public void FireIfDue(DateTimeOffset now)
        {
            if (!_disposed && _dueAt is { } due && due <= now)
            {
                _dueAt = null;
                callback(state);
            }
        }

        public void Dispose() => _disposed = true;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
