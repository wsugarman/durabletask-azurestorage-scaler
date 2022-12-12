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
    private readonly MapField<string, string> _mapField;

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
        => _mapField = mapField ?? throw new ArgumentNullException(nameof(mapField));

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
            => new NullConfigurationSection(key, Key);
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

        public NullConfigurationSection(string key, string parentPath)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Path = Key + ':' + parentPath;
        }

        public IEnumerable<IConfigurationSection> GetChildren()
            => Enumerable.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken()
            => NullChangeToken.Instance;

        public IConfigurationSection GetSection(string key)
            => new NullConfigurationSection(key, Path);
    }

    private sealed class NullChangeToken : IChangeToken
    {
        public static IChangeToken Instance { get; } = new NullChangeToken();

        public bool ActiveChangeCallbacks => true;

        public bool HasChanged => false;

        private NullChangeToken()
        { }

        public IDisposable RegisterChangeCallback(Action<object> callback, object? state)
            => NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static IDisposable Instance { get; } = new NullDisposable();

        private NullDisposable()
        { }

        public void Dispose()
        { }
    }
}
