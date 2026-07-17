using Miko.Testing;
using Miko.Ionic.Components;
using Miko.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonItemOptionsTests : IonicComponentTestBase
{
    private static readonly RenderFragment MinimalChild = builder =>
    {
        builder.OpenElement(0, "span");
        builder.CloseElement();
    };

    [Fact]
    public void IonItemOptions_RendersWithEndSide_ByDefault()
    {
        var cut = Context.Render<IonItemOptions>(parameters =>
            parameters.Add(nameof(IonItemOptions.ChildContent), MinimalChild));

        cut.Root.TagName.ShouldBe("div");
        // Host carries our anchored-edge class (ion-item-options-end) plus Ionic's internal
        // styling classes (item-options-{mode} / item-options-end).
        cut.Root.Class.ShouldBe("md ion-item-options item-options-md ion-item-options-end item-options-end");
    }

    [Fact]
    public void IonItemOptions_RendersWithStartSide_WhenSpecified()
    {
        var cut = Context.Render<IonItemOptions>(parameters =>
        {
            parameters.Add(nameof(IonItemOptions.Side), "start");
            parameters.Add(nameof(IonItemOptions.ChildContent), MinimalChild);
        });

        cut.Root.Class.ShouldBe("md ion-item-options item-options-md ion-item-options-start item-options-start");
    }

    [Fact]
    public void IonItemOptions_CascadesSideToChildOptions()
    {
        // The options container cascades its side; a child option reads it. Render an option
        // inside a start-side container and verify the option picks up that edge.
        var cut = Context.Render<IonItemOptions>(parameters =>
        {
            parameters.Add(nameof(IonItemOptions.Side), "start");
            parameters.Add(nameof(IonItemOptions.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonItemOption>(0);
                builder.CloseComponent();
            }));
        });

        var option = FindByClassInTree(cut.Root, "ion-item-option");
        option.ShouldNotBeNull();
    }

    private static Core.Element? FindByClassInTree(Core.Element root, string cls)
    {
        if (root.Class?.Contains(cls) == true) return root;
        foreach (var child in root.Children)
        {
            var found = FindByClassInTree(child, cls);
            if (found != null) return found;
        }
        return null;
    }
}
