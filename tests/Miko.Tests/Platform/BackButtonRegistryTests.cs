using Miko.Platform;
using Shouldly;
using Xunit;

namespace Miko.Tests.Platform;

/// <summary>
/// Tests for <see cref="BackButtonRegistry"/>, the dispatch chain that carries the system back
/// button (Android hardware back, iOS edge gesture, desktop Escape, simulator panel button) to the
/// overlays that can consume it (ISSUE-138).
/// </summary>
public class BackButtonRegistryTests
{
    [Fact]
    public void Dispatch_WithNoHandlers_ReturnsFalse()
    {
        var registry = new BackButtonRegistry();

        registry.Dispatch().ShouldBeFalse();
    }

    [Fact]
    public void Dispatch_InvokesRegisteredHandler_AndReportsConsumed()
    {
        var registry = new BackButtonRegistry();
        var calls = 0;
        registry.Register(() => { calls++; return true; });

        registry.Dispatch().ShouldBeTrue();

        calls.ShouldBe(1);
    }

    [Fact]
    public void Dispatch_HandlerDeclining_ReturnsFalse()
    {
        var registry = new BackButtonRegistry();
        registry.Register(() => false);

        registry.Dispatch().ShouldBeFalse();
    }

    /// <summary>
    /// The topmost overlay must win: a menu opened over a modal closes first, and the modal stays.
    /// Registration order is layer order, so the *last* handler runs first.
    /// </summary>
    [Fact]
    public void Dispatch_ConsultsMostRecentlyRegisteredHandlerFirst()
    {
        var registry = new BackButtonRegistry();
        var order = new List<string>();
        registry.Register(() => { order.Add("modal"); return true; });
        registry.Register(() => { order.Add("menu"); return true; });

        registry.Dispatch().ShouldBeTrue();

        order.ShouldBe(new[] { "menu" });
    }

    /// <summary>
    /// A declining top handler must fall through to the one beneath it rather than ending dispatch.
    /// </summary>
    [Fact]
    public void Dispatch_FallsThroughDecliningHandlersToTheNextOne()
    {
        var registry = new BackButtonRegistry();
        var order = new List<string>();
        registry.Register(() => { order.Add("modal"); return true; });
        registry.Register(() => { order.Add("menu"); return false; });

        registry.Dispatch().ShouldBeTrue();

        order.ShouldBe(new[] { "menu", "modal" });
    }

    [Fact]
    public void Dispatch_StopsAtFirstConsumingHandler()
    {
        var registry = new BackButtonRegistry();
        var bottomCalls = 0;
        registry.Register(() => { bottomCalls++; return true; });
        registry.Register(() => true);
        registry.Register(() => true);

        registry.Dispatch().ShouldBeTrue();

        bottomCalls.ShouldBe(0);
    }

    [Fact]
    public void Dispose_UnregistersHandler()
    {
        var registry = new BackButtonRegistry();
        var calls = 0;
        var registration = registry.Register(() => { calls++; return true; });

        registration.Dispose();
        registry.Dispatch();

        calls.ShouldBe(0);
        registry.Count.ShouldBe(0);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var registry = new BackButtonRegistry();
        var registration = registry.Register(() => true);

        registration.Dispose();
        registration.Dispose();

        registry.Count.ShouldBe(0);
    }

    [Fact]
    public void Count_TracksRegistrations()
    {
        var registry = new BackButtonRegistry();

        var first = registry.Register(() => true);
        var second = registry.Register(() => true);
        registry.Count.ShouldBe(2);

        first.Dispose();
        registry.Count.ShouldBe(1);

        second.Dispose();
        registry.Count.ShouldBe(0);
    }

    /// <summary>
    /// A handler that closes itself (the common case — a modal dismissing) unregisters mid-dispatch.
    /// Dispatching over a live list would then skip the next entry; the snapshot must prevent that.
    /// </summary>
    [Fact]
    public void Dispatch_HandlerUnregisteringItself_DoesNotSkipOtherHandlers()
    {
        var registry = new BackButtonRegistry();
        var order = new List<string>();
        IDisposable? self = null;
        registry.Register(() => { order.Add("modal"); return true; });
        self = registry.Register(() =>
        {
            order.Add("menu");
            self!.Dispose();
            return false;
        });

        registry.Dispatch().ShouldBeTrue();

        order.ShouldBe(new[] { "menu", "modal" });
    }

    /// <summary>
    /// A handler already unregistered by an earlier handler in the same dispatch must not run —
    /// otherwise a dismissed overlay would react a second time to the same back event.
    /// </summary>
    [Fact]
    public void Dispatch_SkipsHandlerUnregisteredEarlierInTheSameDispatch()
    {
        var registry = new BackButtonRegistry();
        var bottomCalls = 0;
        IDisposable? bottom = null;
        bottom = registry.Register(() => { bottomCalls++; return false; });
        registry.Register(() => { bottom!.Dispose(); return false; });

        registry.Dispatch().ShouldBeFalse();

        bottomCalls.ShouldBe(0);
    }

    /// <summary>
    /// Priority beats order: a page's back button registered after an open modal (because the
    /// header re-rendered) must still lose to the modal.
    /// </summary>
    [Fact]
    public void Dispatch_HigherPriorityWins_EvenWhenRegisteredEarlier()
    {
        var registry = new BackButtonRegistry();
        var order = new List<string>();
        registry.Register(() => { order.Add("modal"); return true; }, BackButtonPriority.Overlay);
        registry.Register(() => { order.Add("menu"); return true; }, BackButtonPriority.Menu);
        registry.Register(() => { order.Add("nav"); return true; }, BackButtonPriority.Navigation);

        registry.Dispatch().ShouldBeTrue();

        order.ShouldBe(new[] { "modal" });
    }

    [Fact]
    public void Dispatch_FallsThroughPrioritiesInDescendingOrder()
    {
        var registry = new BackButtonRegistry();
        var order = new List<string>();
        registry.Register(() => { order.Add("nav"); return true; }, BackButtonPriority.Navigation);
        registry.Register(() => { order.Add("modal"); return false; }, BackButtonPriority.Overlay);
        registry.Register(() => { order.Add("menu"); return false; }, BackButtonPriority.Menu);

        registry.Dispatch().ShouldBeTrue();

        order.ShouldBe(new[] { "modal", "menu", "nav" });
    }

    [Fact]
    public void Dispatch_SamePriority_LatestRegistrationWins()
    {
        var registry = new BackButtonRegistry();
        var order = new List<string>();
        registry.Register(() => { order.Add("first"); return true; }, BackButtonPriority.Overlay);
        registry.Register(() => { order.Add("second"); return true; }, BackButtonPriority.Overlay);

        registry.Dispatch();

        order.ShouldBe(new[] { "second" });
    }

    /// <summary>
    /// The Android host claims the system back only while handlers exist, so it must hear about
    /// every addition and removal — and not about a no-op double dispose.
    /// </summary>
    [Fact]
    public void Changed_IsRaisedOnRegisterAndOnFirstDisposeOnly()
    {
        var registry = new BackButtonRegistry();
        var counts = new List<int>();
        registry.Changed += () => counts.Add(registry.Count);

        var registration = registry.Register(() => true);
        registration.Dispose();
        registration.Dispose();

        counts.ShouldBe(new[] { 1, 0 });
    }

    [Fact]
    public void Register_NullHandler_Throws()
    {
        var registry = new BackButtonRegistry();

        Should.Throw<ArgumentNullException>(() => registry.Register(null!));
    }
}
