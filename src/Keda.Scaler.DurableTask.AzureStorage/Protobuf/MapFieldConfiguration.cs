// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Protobuf;

internal sealed class MapFieldConfiguration : IConfiguration
{
    private readonly Dictionary<string, string> _mapField;

    public string? this[string key]
    {
        get => _mapField.TryGetValue(key, out string? value) ? value : null;
        set
        {
            if (value is null)
                _mapField.Remove(key);
            else
                _mapField[key] = value;
        }
    }

    public MapFieldConfiguration(MapField<string, string> mapField)
    {
        if (mapField is null)
            throw new ArgumentNullException(nameof(mapField));

        // MapField<TKey, TValue> uses StringComparer.Ordinal and cannot be changed.
        // However, IConfiguration object use case-insensitive keys, so the values must be copied
        // into a different data structure so they can be properly queried.
        _mapField = new Dictionary<string, string>(mapField, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<IConfigurationSection> GetChildren()
        => _mapField.Keys.Select(k => new MapFieldConfigurationSection(k, this));

    public IChangeToken GetReloadToken()
        => NullChangeToken.Instance;

    public IConfigurationSection GetSection(string key)
        => new MapFieldConfigurationSection(key ?? throw new ArgumentNullException(nameof(key)), this);

    private sealed class MapFieldConfigurationSection : IConfigurationSection
    {
        public string? this[string key]
        {
            get => null;
            set => throw new NotSupportedException();
        }

        public string Key { get; }

        public string Path => Key;

        public string? Value
        {
            get => _configuration[Key];
            set => _configuration[Key] = value;
        }

        private readonly MapFieldConfiguration _configuration;

        public MapFieldConfigurationSection(string key, MapFieldConfiguration configuration)
        {
            Key = key;
            _configuration = configuration;
        }

        public IEnumerable<IConfigurationSection> GetChildren()
            => Enumerable.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken()
            => NullChangeToken.Instance;

        public IConfigurationSection GetSection(string key)
            => new NullConfigurationSection(Path, key);
    }

    private sealed class NullConfigurationSection : IConfigurationSection
    {
        public string? this[string key]
        {
            get => null;
            set => throw new NotSupportedException();
        }

        public string Key { get; }

        public string Path { get; }

        public string? Value
        {
            get => null;
            set => throw new NotSupportedException();
        }

        public NullConfigurationSection(string parentPath, string key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Path = parentPath + ':' + key;
        }

        public IEnumerable<IConfigurationSection> GetChildren()
            => Enumerable.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken()
            => NullChangeToken.Instance;

        public IConfigurationSection GetSection(string key)
            => new NullConfigurationSection(Path, key);
    }

    private sealed class NullChangeToken : IChangeToken
    {
        public static NullChangeToken Instance { get; } = new NullChangeToken();

        public bool ActiveChangeCallbacks => true;

        public bool HasChanged => false;

        private NullChangeToken()
        { }

        public IDisposable RegisterChangeCallback(Action<object> callback, object? state)
            => NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static NullDisposable Instance { get; } = new NullDisposable();

        private NullDisposable()
        { }

        public void Dispose()
        { }
    }
}
