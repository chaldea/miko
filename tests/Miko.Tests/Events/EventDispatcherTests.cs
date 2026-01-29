using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Shouldly;

namespace Miko.Tests.Events;

public class EventDispatcherTests
{
    private readonly EventDispatcher _dispatcher = new();

    [Fact]
    public void Dispatch_ShouldInvokeHandler()
    {
        var element = new ButtonElement();
        var invoked = false;

        element.AddEventListener<MouseEventArgs>(EventTypes.Click, _ => invoked = true);

        _dispatcher.Dispatch(element, EventTypes.Click,
            new MouseEventArgs { Target = element });

        invoked.ShouldBeTrue();
    }

    [Fact]
    public void Dispatch_ShouldInvokeConvenienceHandler()
    {
        var element = new ButtonElement();
        var invoked = false;

        element.OnClick = _ => invoked = true;

        _dispatcher.Dispatch(element, EventTypes.Click,
            new MouseEventArgs { Target = element });

        invoked.ShouldBeTrue();
    }

    [Fact]
    public void Dispatch_ShouldBubbleToParent()
    {
        var parent = new DivElement();
        var child = new ButtonElement();
        parent.AddChild(child);

        var parentInvoked = false;
        parent.AddEventListener<MouseEventArgs>(EventTypes.Click, _ => parentInvoked = true);

        _dispatcher.Dispatch(child, EventTypes.Click,
            new MouseEventArgs { Target = child });

        parentInvoked.ShouldBeTrue();
    }

    [Fact]
    public void Dispatch_ShouldBubbleThroughMultipleLevels()
    {
        var grandparent = new DivElement();
        var parent = new DivElement();
        var child = new ButtonElement();
        grandparent.AddChild(parent);
        parent.AddChild(child);

        var grandparentInvoked = false;
        grandparent.AddEventListener<MouseEventArgs>(EventTypes.Click, _ => grandparentInvoked = true);

        _dispatcher.Dispatch(child, EventTypes.Click,
            new MouseEventArgs { Target = child });

        grandparentInvoked.ShouldBeTrue();
    }

    [Fact]
    public void Dispatch_WithStopPropagation_ShouldNotBubble()
    {
        var parent = new DivElement();
        var child = new ButtonElement();
        parent.AddChild(child);

        var parentInvoked = false;
        child.AddEventListener<MouseEventArgs>(EventTypes.Click, args => args.StopPropagation());
        parent.AddEventListener<MouseEventArgs>(EventTypes.Click, _ => parentInvoked = true);

        _dispatcher.Dispatch(child, EventTypes.Click,
            new MouseEventArgs { Target = child });

        parentInvoked.ShouldBeFalse();
    }

    [Fact]
    public void Dispatch_WithBubblesFalse_ShouldNotBubble()
    {
        var parent = new DivElement();
        var child = new ButtonElement();
        parent.AddChild(child);

        var parentInvoked = false;
        parent.AddEventListener<MouseEventArgs>(EventTypes.MouseEnter, _ => parentInvoked = true);

        _dispatcher.Dispatch(child, EventTypes.MouseEnter,
            new MouseEventArgs { Target = child, Bubbles = false });

        parentInvoked.ShouldBeFalse();
    }

    [Fact]
    public void Dispatch_OnDisabledElement_ShouldNotInvokeHandler()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Disabled);
        var invoked = false;

        element.AddEventListener<MouseEventArgs>(EventTypes.Click, _ => invoked = true);

        _dispatcher.Dispatch(element, EventTypes.Click,
            new MouseEventArgs { Target = element });

        invoked.ShouldBeFalse();
    }

    [Fact]
    public void Dispatch_MouseLeaveOnDisabledElement_ShouldStillInvoke()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Disabled);
        var invoked = false;

        element.AddEventListener<MouseEventArgs>(EventTypes.MouseLeave, _ => invoked = true);

        _dispatcher.Dispatch(element, EventTypes.MouseLeave,
            new MouseEventArgs { Target = element });

        invoked.ShouldBeTrue();
    }

    [Fact]
    public void Dispatch_ShouldSetCurrentTarget()
    {
        var parent = new DivElement();
        var child = new ButtonElement();
        parent.AddChild(child);

        Element? capturedCurrentTarget = null;
        parent.AddEventListener<MouseEventArgs>(EventTypes.Click,
            args => capturedCurrentTarget = args.CurrentTarget);

        _dispatcher.Dispatch(child, EventTypes.Click,
            new MouseEventArgs { Target = child });

        capturedCurrentTarget.ShouldBe(parent);
    }

    [Fact]
    public void Dispatch_TargetShouldRemainConstant()
    {
        var parent = new DivElement();
        var child = new ButtonElement();
        parent.AddChild(child);

        Element? capturedTarget = null;
        parent.AddEventListener<MouseEventArgs>(EventTypes.Click,
            args => capturedTarget = args.Target);

        _dispatcher.Dispatch(child, EventTypes.Click,
            new MouseEventArgs { Target = child });

        capturedTarget.ShouldBe(child);
    }

    [Fact]
    public void Dispatch_ShouldInvokeMultipleHandlers()
    {
        var element = new ButtonElement();
        var count = 0;

        element.AddEventListener<MouseEventArgs>(EventTypes.Click, _ => count++);
        element.AddEventListener<MouseEventArgs>(EventTypes.Click, _ => count++);

        _dispatcher.Dispatch(element, EventTypes.Click,
            new MouseEventArgs { Target = element });

        count.ShouldBe(2);
    }

    [Fact]
    public void RemoveEventListener_ShouldRemoveHandler()
    {
        var element = new ButtonElement();
        var invoked = false;
        MikoEventHandler<MouseEventArgs> handler = _ => invoked = true;

        element.AddEventListener(EventTypes.Click, handler);
        element.RemoveEventListener(EventTypes.Click, handler);

        _dispatcher.Dispatch(element, EventTypes.Click,
            new MouseEventArgs { Target = element });

        invoked.ShouldBeFalse();
    }

    [Fact]
    public void Dispatch_FocusEvent_ShouldInvokeOnFocus()
    {
        var element = new InputElement();
        var invoked = false;

        element.OnFocus = _ => invoked = true;

        _dispatcher.Dispatch(element, EventTypes.Focus,
            new FocusEventArgs { Target = element });

        invoked.ShouldBeTrue();
    }

    [Fact]
    public void Dispatch_BlurEvent_ShouldInvokeOnBlur()
    {
        var element = new InputElement();
        var invoked = false;

        element.OnBlur = _ => invoked = true;

        _dispatcher.Dispatch(element, EventTypes.Blur,
            new FocusEventArgs { Target = element });

        invoked.ShouldBeTrue();
    }

    [Fact]
    public void Dispatch_ChangeEvent_ShouldInvokeOnChange()
    {
        var element = new InputElement();
        var invoked = false;

        element.OnChange = _ => invoked = true;

        _dispatcher.Dispatch(element, EventTypes.Change,
            new ChangeEventArgs { Target = element, OldValue = "old", NewValue = "new" });

        invoked.ShouldBeTrue();
    }

    [Fact]
    public void PreventDefault_ShouldSetFlag()
    {
        var element = new ButtonElement();
        var args = new MouseEventArgs { Target = element };

        args.DefaultPrevented.ShouldBeFalse();

        args.PreventDefault();

        args.DefaultPrevented.ShouldBeTrue();
    }
}
