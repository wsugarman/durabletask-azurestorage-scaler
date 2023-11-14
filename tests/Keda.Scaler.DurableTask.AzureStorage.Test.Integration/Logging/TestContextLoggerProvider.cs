// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration.Logging;

[ProviderAlias("MSTest")]
internal sealed class TestContextLoggerProvider : ILoggerProvider
{
    private readonly TestContext _testContext;

    public TestContextLoggerProvider(TestContext testContext)
        => _testContext = testContext ?? throw new ArgumentNullException(nameof(testContext));

    public ILogger CreateLogger(string categoryName)
        => new TestContextLogger(categoryName, _testContext);

    public void Dispose()
    { }
}
