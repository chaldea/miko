using Miko.Animation;
using Miko.Common;
using Shouldly;

namespace Miko.Tests.Animation;

public class TransformTests
{
    [Fact]
    public void None_ShouldHaveEmptyFunctions()
    {
        var transform = Transform.None;
        transform.Functions.ShouldBeEmpty();
    }

    [Fact]
    public void FromTranslate_ShouldCreateTranslateTransform()
    {
        var transform = Transform.FromTranslate(Length.Px(10), Length.Px(20));
        transform.Functions.Count.ShouldBe(1);
        transform.Functions[0].ShouldBeOfType<TransformFunction.Translate>();

        var translate = (TransformFunction.Translate)transform.Functions[0];
        translate.X.Value.ShouldBe(10f);
        translate.Y.Value.ShouldBe(20f);
    }

    [Fact]
    public void FromScale_ShouldCreateScaleTransform()
    {
        var transform = Transform.FromScale(2f, 3f);
        transform.Functions.Count.ShouldBe(1);

        var scale = (TransformFunction.Scale)transform.Functions[0];
        scale.X.ShouldBe(2f);
        scale.Y.ShouldBe(3f);
    }

    [Fact]
    public void FromRotate_ShouldCreateRotateTransform()
    {
        var transform = Transform.FromRotate(45f);
        transform.Functions.Count.ShouldBe(1);

        var rotate = (TransformFunction.Rotate)transform.Functions[0];
        rotate.Degrees.ShouldBe(45f);
    }

    [Fact]
    public void MultipleTransforms_ShouldChain()
    {
        var transform = new Transform(
            new TransformFunction.Translate(Length.Px(10), Length.Px(0)),
            new TransformFunction.Rotate(90f),
            new TransformFunction.Scale(2f, 2f)
        );

        transform.Functions.Count.ShouldBe(3);
        transform.Functions[0].ShouldBeOfType<TransformFunction.Translate>();
        transform.Functions[1].ShouldBeOfType<TransformFunction.Rotate>();
        transform.Functions[2].ShouldBeOfType<TransformFunction.Scale>();
    }

    [Fact]
    public void TransformOrigin_Center_ShouldBe50Percent()
    {
        var origin = TransformOrigin.Center;
        origin.X.Value.ShouldBe(50f);
        origin.X.Unit.ShouldBe(LengthUnit.Percent);
        origin.Y.Value.ShouldBe(50f);
        origin.Y.Unit.ShouldBe(LengthUnit.Percent);
    }

    [Fact]
    public void TransformOrigin_TopLeft_ShouldBe0Percent()
    {
        var origin = TransformOrigin.TopLeft;
        origin.X.Value.ShouldBe(0f);
        origin.Y.Value.ShouldBe(0f);
    }
}
