using System;
using Microsoft.Extensions.Options;

namespace PiholeDnsPropagate.Tests.Teleporter.Fixtures;

internal sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    where T : class, new()
{
    private T _currentValue;

    public TestOptionsMonitor(T currentValue)
    {
        _currentValue = currentValue;
    }

    public T CurrentValue => _currentValue;

    public T Get(string? name) => _currentValue;

    public IDisposable? OnChange(Action<T, string?> listener)
    {
        return null;
    }

    public void Update(T value) => _currentValue = value;
}
