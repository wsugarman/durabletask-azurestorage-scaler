// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

internal static class AssertExtensions
{
    public static T ThrowsDerivedException<T>(this Assert assert, Action action)
        where T : Exception
    {
        return assert.ThrowsDerivedException<T>(
            () =>
            {
                action();
                return default;
            });
    }

    public static T ThrowsDerivedException<T>(this Assert assert, Func<object?> func)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(assert);
        ArgumentNullException.ThrowIfNull(func);

        try
        {
            _ = func();
            Assert.Fail("Expected an exception of type '{0}' but no exception was thrown", typeof(T).Name);
            return default;
        }
        catch (T expected)
        {
            return expected;
        }
        catch (Exception other)
        {
            Assert.Fail("Expected an exception of type '{0}' but an exception of type '{1}' was thrown instead", typeof(T).Name, other.GetType().Name);
            throw;
        }
    }
}
