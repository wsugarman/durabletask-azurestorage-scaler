// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Collections;
using Keda.Scaler.DurableTask.AzureStorage.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Protobuf;

[TestClass]
public class MapFieldConfigurationTest
{
    [TestMethod]
    public void IndexerGet()
    {
        IConfiguration config = new MapFieldConfiguration(
            new MapField<string, string>
            {
                { "one", "1" },
                { "two", "2" },
            });

        Assert.AreEqual(null, config["three"]);
        Assert.AreEqual("1", config["One"]);
    }

    [TestMethod]
    public void IndexerSet()
    {
        IConfiguration config = new MapFieldConfiguration(
            new MapField<string, string>
            {
                { "one", "1" },
                { "two", "2" },
            });

        Assert.AreEqual("1", config["one"]);
        Assert.AreEqual("2", config["tWO"]);

        config["one"] = "un";
        config["three"] = "trois";
        config["two"] = null;
        config["four"] = null;

        Assert.AreEqual("un", config["onE"]);
        Assert.AreEqual("trois", config["thRee"]);
        Assert.AreEqual(null, config["two"]);
        Assert.AreEqual(null, config["four"]);
    }

    [TestMethod]
    public void CtorExceptions()
        => Assert.ThrowsException<ArgumentNullException>(() => new MapFieldConfiguration(null!));

    [TestMethod]
    public void GetChildren()
    {
        IConfiguration config = new MapFieldConfiguration(
            new MapField<string, string>
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" },
            });

        Dictionary<string, string?> children = config.GetChildren().ToDictionary(x => x.Key, x => x.Value);
        Assert.AreEqual(3, children.Count);
        Assert.AreEqual("1", children["one"]);
        Assert.AreEqual("2", children["two"]);
        Assert.AreEqual("3", children["three"]);
    }

    [TestMethod]
    public void GetReloadToken()
    {
        IConfiguration config = new MapFieldConfiguration(new MapField<string, string> { { "foo", "bar" } });

        IChangeToken token = config.GetReloadToken();
        Assert.IsTrue(token.ActiveChangeCallbacks);
        Assert.IsFalse(token.HasChanged);

        // Assert the token isn't actively monitoring changes
        int calls = 0;
        IDisposable lease = token.RegisterChangeCallback(o => calls++, null);
        config["hello"] = "world";
        Assert.AreEqual(0, calls);

        // Dispose can be called multiple times without issue
        lease.Dispose();
        lease.Dispose();
    }

    [TestMethod]
    public void GetSection()
    {
        IConfiguration config = new MapFieldConfiguration(
            new MapField<string, string>
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" },
            });

        // Invalid key
        Assert.ThrowsException<ArgumentNullException>(() => config.GetSection(null!));

        // Missing
        IConfigurationSection missing = config.GetSection("four");
        Assert.AreEqual("four", missing.Key);
        Assert.AreEqual("four", missing.Path);
        Assert.AreEqual(null, missing.Value);
        Assert.AreEqual(0, missing.GetChildren().Count());
        Assert.IsNotNull(missing.GetReloadToken());
        Assert.IsNull(missing["any key"]);

        // Found
        IConfigurationSection section = config.GetSection("TWo");
        Assert.AreEqual("TWo", section.Key);
        Assert.AreEqual("TWo", section.Path);
        Assert.AreEqual("2", section.Value);
        Assert.AreEqual(0, section.GetChildren().Count());
        Assert.IsNotNull(section.GetReloadToken());
        Assert.IsNull(section["any key"]);

        // Nested
        IConfigurationSection nested = section.GetSection("TWO");
        Assert.AreEqual("TWO", nested.Key);
        Assert.AreEqual("TWo:TWO", nested.Path);
        Assert.AreEqual(null, nested.Value);
        Assert.AreEqual(0, nested.GetChildren().Count());
        Assert.IsNotNull(nested.GetReloadToken());
        Assert.IsNull(nested["any key"]);

        // Invalid nested
        Assert.ThrowsException<ArgumentNullException>(() => nested.GetSection(null!));

        // Modify the sections
        missing.Value = "quatre";
        section.Value = "deux";
        Assert.ThrowsException<NotSupportedException>(() => nested.Value = "vingt-deux");

        Assert.AreEqual("quatre", missing.Value);
        Assert.AreEqual("deux", section.Value);

        // Can't modify by key (as there is only 1 level of keys for map fields)
        Assert.ThrowsException<NotSupportedException>(() => missing["foo"] = "foo");
        Assert.ThrowsException<NotSupportedException>(() => section["bar"] = "bar");
        Assert.ThrowsException<NotSupportedException>(() => nested["baz"] = "baz");

        // Nested-Nested
        IConfigurationSection subNested = nested.GetSection("two");
        Assert.AreEqual("two", subNested.Key);
        Assert.AreEqual("TWo:TWO:two", subNested.Path);
        Assert.AreEqual(null, subNested.Value);
        Assert.AreEqual(0, subNested.GetChildren().Count());
        Assert.IsNotNull(subNested.GetReloadToken());
        Assert.IsNull(subNested["any key"]);
    }
}
