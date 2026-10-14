using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Styling;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Haptics of the reorder gesture (ISSUE-149): pressing a handle is felt as a light tap and opens a
/// selection, every change of the target slot ticks once, and releasing closes the selection.
/// Items are 20px tall, so the target moves one slot per 20px of drag once past the first half row.
/// </summary>
public class IonReorderHapticsTests : IonicComponentTestBase
{
    private readonly EventDispatcher _dispatcher = new();

    private static RenderFragment Items(int count) => builder =>
    {
        for (var index = 0; index < count; index++)
        {
            builder.OpenElement(index * 3, "div");
            builder.AddAttribute(index * 3 + 1, "style", new Style { Height = Length.Px(20) });
            builder.OpenComponent<IonReorder>(index * 3 + 2);
            builder.CloseComponent();
            builder.CloseElement();
        }
    };

    private Element RenderEnabledGroup()
    {
        var cut = Context.Render<IonReorderGroup>(p =>
        {
            p.Add(nameof(IonReorderGroup.Disabled), false);
            p.Add(nameof(IonReorderGroup.ChildContent), Items(3));
        });
        return cut.FindByClass("ion-reorder")[0];
    }

    private void Mouse(Element target, string type, float y, bool pressed)
        => _dispatcher.Dispatch(target, type, new MouseEventArgs
        {
            Target = target,
            Y = y,
            IsButtonPressed = pressed,
            Button = MouseButton.Left,
            TargetHeight = target.OffsetHeight,
        });

    [Fact]
    public void PressingAHandle_TapsLightly_AndOpensASelection()
    {
        var handle = RenderEnabledGroup();

        Mouse(handle, EventTypes.MouseDown, 10, pressed: true);

        Haptics.Calls.ShouldBe(new[] { "impact:Light", "start" });
    }

    [Fact]
    public void Dragging_TicksOncePerSlotChange()
    {
        var handle = RenderEnabledGroup();
        Mouse(handle, EventTypes.MouseDown, 10, pressed: true);

        Mouse(handle, EventTypes.MouseMove, 25, pressed: true);   // +15: still over its own slot
        Haptics.Count("changed").ShouldBe(0);

        Mouse(handle, EventTypes.MouseMove, 31, pressed: true);   // +21: over slot 1
        Mouse(handle, EventTypes.MouseMove, 35, pressed: true);   // same slot, no new tick
        Haptics.Count("changed").ShouldBe(1);

        Mouse(handle, EventTypes.MouseMove, 55, pressed: true);   // +45: over slot 2
        Mouse(handle, EventTypes.MouseMove, 20, pressed: true);   // back over its own slot
        Haptics.Count("changed").ShouldBe(3);
    }

    [Fact]
    public void Releasing_ClosesTheSelection()
    {
        var handle = RenderEnabledGroup();
        Mouse(handle, EventTypes.MouseDown, 10, pressed: true);
        Mouse(handle, EventTypes.MouseMove, 31, pressed: true);

        Mouse(handle, EventTypes.MouseUp, 31, pressed: false);

        Haptics.Calls.ShouldBe(new[] { "impact:Light", "start", "changed", "end" });
    }

    [Fact]
    public void DisabledGroup_IsSilent()
    {
        var cut = Context.Render<IonReorderGroup>(p => p.Add(nameof(IonReorderGroup.ChildContent), Items(2)));
        var handle = cut.FindByClass("ion-reorder")[0];

        Mouse(handle, EventTypes.MouseDown, 10, pressed: true);
        Mouse(handle, EventTypes.MouseMove, 40, pressed: true);
        Mouse(handle, EventTypes.MouseUp, 40, pressed: false);

        Haptics.Calls.ShouldBeEmpty();
    }

    /// <summary>Removes its group on demand, as leaving the page mid-drag does.</summary>
    private sealed class GroupHost : ComponentBase
    {
        private bool _show = true;

        [Parameter] public Action<GroupHost>? Created { get; set; }

        protected override void OnInitialized() => Created?.Invoke(this);

        public void Hide()
        {
            _show = false;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            if (_show)
            {
                var group = builder.OpenComponent<IonReorderGroup>();
                group.Disabled = false;
                group.ChildContent = Items(3);
                builder.CloseComponent();
            }
            builder.CloseElement();
        }
    }

    [Fact]
    public void DisposingTheGroupMidDrag_ClosesTheSelection()
    {
        GroupHost? host = null;
        var cut = Context.Render<GroupHost>(p => p.Add(nameof(GroupHost.Created), (Action<GroupHost>)(h => host = h)));
        var handle = cut.FindByClass("ion-reorder")[0];
        Mouse(handle, EventTypes.MouseDown, 10, pressed: true);

        host!.Hide();

        Haptics.Calls.ShouldBe(new[] { "impact:Light", "start", "end" });
    }
}
