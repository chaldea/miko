using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests;

/// <summary>
/// Base class for Ionic component tests providing common test utilities.
/// </summary>
public abstract class IonicComponentTestBase : IDisposable
{
    protected TestContext Context { get; }

    protected IonicComponentTestBase()
    {
        Context = new TestContext();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}
