// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal sealed class Monitored<T> where T : class
{
    public T Current => _value;

    private volatile T _value;
    private readonly Func<T> _factory;

    public Monitored(Func<T> factory, Func<IChangeToken?> changeTokenProducer)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _value = _factory();
        ChangeToken.OnChange(changeTokenProducer, Reload);
    }

    private void Reload()
    {
        lock (_factory)
            _value = _factory();
    }
}
