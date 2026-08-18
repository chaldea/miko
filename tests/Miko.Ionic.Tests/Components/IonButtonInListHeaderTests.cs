using Miko.Components;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonButtonInListHeaderTests : IonicComponentTestBase
{
    private ComponentUnderTest RenderButtonInListHeader(string? fill = null)
    {
        RenderFragment button = builder =>
        {
            builder.OpenComponent<IonButton>(0);
            if (fill is not null)
            {
                builder.AddComponentParameter(1, nameof(IonButton.Fill), fill);
            }
            builder.AddComponentParameter(2, nameof(IonButton.ChildContent),
                (RenderFragment)(content => content.AddContent(0, "Edit")));
            builder.CloseComponent();
        };

        return Context.Render<IonListHeader>(parameters =>
            parameters.Add(nameof(IonListHeader.ChildContent), button));
    }

    [Fact]
    public void ButtonInsideListHeader_DefaultsToClearFill()
    {
        var cut = RenderButtonInListHeader();
        var button = cut.FindByClass("ion-button").ShouldHaveSingleItem();

        button.ShouldHaveClass("button-clear");
        button.ShouldNotHaveClass("button-solid");
        button.ShouldHaveClass("button-fill-default");
    }

    [Fact]
    public void ButtonInsideListHeader_KeepsExplicitFill()
    {
        var cut = RenderButtonInListHeader("solid");
        var button = cut.FindByClass("ion-button").ShouldHaveSingleItem();

        button.ShouldHaveClass("button-solid");
        button.ShouldNotHaveClass("button-clear");
        button.ShouldNotHaveClass("button-fill-default");
    }
}
