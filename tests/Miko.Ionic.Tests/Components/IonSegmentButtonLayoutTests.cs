using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Layout/BoxModel coverage for <c>ion-segment-button</c> content centering (ISSUE-064 §1.2).
/// <para>
/// The button is a flex column with <c>align-items: center</c> (cross axis → horizontal) and
/// <c>justify-content: center</c> (main axis → vertical), and its height comes from
/// <c>min-height</c> rather than an explicit height. Its label must end up centered both ways.
/// The engine bug was that justify-content was skipped while the main-axis size was still 0
/// (min-height was applied to the box only AFTER the children were placed), leaving the label
/// pinned to the top.
/// </para>
/// </summary>
public class IonSegmentButtonLayoutTests : IonicComponentTestBase
{
    public IonSegmentButtonLayoutTests()
    {
        // Load the real Ionic stylesheet so the ported segment-button styles drive layout.
        Context.AddStyleSheet(IonicStyleSheetFactory.Create(IonicTheme.CreateMd()));
    }

    [Fact]
    public void IonSegmentButton_CentersLabel_VerticallyAndHorizontally()
    {
        // Render a single segment button with a label, inside a fixed-width viewport so the
        // button has a definite width to center the label across.
        Context.ViewportWidth = 300f;
        Context.ViewportHeight = 200f;

        var cut = Context.Render<IonSegmentButton>(parameters =>
        {
            parameters.Add(nameof(IonSegmentButton.Value), "all");
            parameters.Add(nameof(IonSegmentButton.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<IonLabel>(0);
                builder.AddComponentParameter(1, nameof(IonLabel.ChildContent), (RenderFragment)(inner =>
                {
                    inner.AddContent(0, "All");
                }));
                builder.CloseComponent();
            }));
        });

        var button = cut.Root; // <button class="ion-segment-button">
        button.TagName.ShouldBe("button");

        var buttonBox = cut.GetBoxModel(button);
        buttonBox.ShouldNotBeNull();

        // The label sits inside the button (ion-label is the first descendant div).
        var label = button.FindByClass("ion-label").FirstOrDefault();
        label.ShouldNotBeNull();
        var labelBox = cut.GetBoxModel(label!);
        labelBox.ShouldNotBeNull();

        // Vertical centering: the label's vertical center should align with the button's
        // vertical center (within a small tolerance for rounding / text metrics).
        float buttonMidY = buttonBox!.Content.Y + buttonBox.Content.Height / 2f;
        float labelMidY = labelBox!.Content.Y + labelBox.Content.Height / 2f;
        Math.Abs(labelMidY - buttonMidY).ShouldBeLessThan(1.0f);

        // The label must NOT be pinned to the top of the button: with min-height (≈48px MD)
        // larger than the label line, the centered label's top is offset below the button top.
        (labelBox.Content.Y - buttonBox.Content.Y).ShouldBeGreaterThan(0f);
    }
}
