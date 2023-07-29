// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal sealed class Monitored<T> : IDisposable
    where T : class
{
    public T Current => _value;

    private volatile T _value;
    private readonly Func<T> _factory;
    private readonly IDisposable _receipt;

    public Monitored(Func<T> factory, Func<IChangeToken?> changeTokenProducer)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _value = _factory();
        _receipt = ChangeToken.OnChange(changeTokenProducer, Reload);
    }

    private void Reload()
    {
        lock (_factory)
            _value = _factory();
    }

    public void Dispose()
    {
        _receipt.Dispose();
        GC.SuppressFinalize(this);
    }
}
