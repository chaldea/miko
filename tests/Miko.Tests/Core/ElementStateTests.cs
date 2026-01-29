using Miko.Core;
using Miko.Core.DomElements;
using Shouldly;

namespace Miko.Tests.Core;

public class ElementStateTests
{
    [Fact]
    public void SetState_ShouldAddStateFlag()
    {
        var element = new ButtonElement();

        element.SetState(ElementState.Hover);

        element.HasState(ElementState.Hover).ShouldBeTrue();
    }

    [Fact]
    public void SetState_ShouldMarkElementAsDirty()
    {
        var element = new ButtonElement();
        element.IsDirty = false;

        element.SetState(ElementState.Hover);

        element.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void SetState_WhenAlreadySet_ShouldNotMarkDirty()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Hover);
        element.IsDirty = false;

        element.SetState(ElementState.Hover);

        element.IsDirty.ShouldBeFalse();
    }

    [Fact]
    public void ClearState_ShouldRemoveStateFlag()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Hover);

        element.ClearState(ElementState.Hover);

        element.HasState(ElementState.Hover).ShouldBeFalse();
    }

    [Fact]
    public void ClearState_ShouldMarkElementAsDirty()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Hover);
        element.IsDirty = false;

        element.ClearState(ElementState.Hover);

        element.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void ClearState_WhenNotSet_ShouldNotMarkDirty()
    {
        var element = new ButtonElement();
        element.IsDirty = false;

        element.ClearState(ElementState.Hover);

        element.IsDirty.ShouldBeFalse();
    }

    [Fact]
    public void MultipleStates_CanCoexist()
    {
        var element = new ButtonElement();

        element.SetState(ElementState.Hover);
        element.SetState(ElementState.Focus);

        element.HasState(ElementState.Hover).ShouldBeTrue();
        element.HasState(ElementState.Focus).ShouldBeTrue();
    }

    [Fact]
    public void ClearState_ShouldOnlyClearSpecifiedState()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Hover);
        element.SetState(ElementState.Focus);

        element.ClearState(ElementState.Hover);

        element.HasState(ElementState.Hover).ShouldBeFalse();
        element.HasState(ElementState.Focus).ShouldBeTrue();
    }

    [Fact]
    public void IsDisabled_WhenDisabledStateSet_ShouldReturnTrue()
    {
        var element = new ButtonElement();

        element.SetState(ElementState.Disabled);

        element.IsDisabled.ShouldBeTrue();
    }

    [Fact]
    public void IsDisabled_WhenNoDisabledState_ShouldReturnFalse()
    {
        var element = new ButtonElement();

        element.IsDisabled.ShouldBeFalse();
    }

    [Fact]
    public void IsDisabled_ShouldCheckParentChain()
    {
        var parent = new DivElement();
        var child = new ButtonElement();
        parent.AddChild(child);

        parent.SetState(ElementState.Disabled);

        child.IsDisabled.ShouldBeTrue();
    }

    [Fact]
    public void IsDisabled_WithEnabledParent_ShouldReturnFalse()
    {
        var parent = new DivElement();
        var child = new ButtonElement();
        parent.AddChild(child);

        child.IsDisabled.ShouldBeFalse();
    }

    [Fact]
    public void IsDisabled_ShouldCheckGrandparentChain()
    {
        var grandparent = new DivElement();
        var parent = new DivElement();
        var child = new ButtonElement();
        grandparent.AddChild(parent);
        parent.AddChild(child);

        grandparent.SetState(ElementState.Disabled);

        child.IsDisabled.ShouldBeTrue();
    }

    [Fact]
    public void State_InitialValue_ShouldBeNone()
    {
        var element = new ButtonElement();

        element.State.ShouldBe(ElementState.None);
    }

    [Fact]
    public void HasState_WithCombinedStates_ShouldCheckAllFlags()
    {
        var element = new ButtonElement();
        element.SetState(ElementState.Hover);
        element.SetState(ElementState.Active);

        // 检查单个状态
        element.HasState(ElementState.Hover).ShouldBeTrue();
        element.HasState(ElementState.Active).ShouldBeTrue();

        // 检查组合状态
        element.HasState(ElementState.Hover | ElementState.Active).ShouldBeTrue();

        // 检查不存在的状态
        element.HasState(ElementState.Focus).ShouldBeFalse();
    }
}
