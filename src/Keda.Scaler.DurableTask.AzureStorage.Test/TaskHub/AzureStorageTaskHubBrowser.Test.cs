// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public class AzureStorageTaskHubBrowserTest
{
    private readonly AzureStorageTaskHubBrowser _browser;

    public AzureStorageTaskHubBrowserTest()
        => _browser = new AzureStorageTaskHubBrowser(NullLoggerFactory.Instance);

    [TestMethod]
    public void CtorExceptions()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new AzureStorageTaskHubBrowser(null!));

        Mock<ILoggerFactory> mockFactory = new Mock<ILoggerFactory>();
        mockFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns<ILogger>(null);
        Assert.ThrowsException<ArgumentNullException>(() => new AzureStorageTaskHubBrowser(mockFactory.Object));
    }

    [TestMethod]
    [DataRow]
    public void GetMonitorAsync(string? connectionString, string? serviceEndpoint, bool useManagedIdentity)
    {

    }
}
