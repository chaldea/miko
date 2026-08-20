using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

public class IonInputOtpTests : IonicComponentTestBase
{
    private static readonly RenderFragment Description = builder =>
    {
        builder.AddContent(0, "Didn't get a code? ");
        builder.OpenElement(1, "a");
        builder.AddContent(2, "Resend the code");
        builder.CloseElement();
    };

    private static ComponentUnderTest RenderOtp(
        TestContext context,
        Action<ComponentParameterBuilder<IonInputOtp>>? configure = null,
        bool withDescription = true)
        => context.Render<IonInputOtp>(parameters =>
        {
            if (withDescription)
                parameters.Add(nameof(IonInputOtp.ChildContent), Description);
            configure?.Invoke(parameters);
        });

    [Fact]
    public void IonInputOtp_RendersDefaultDomContract()
    {
        var cut = RenderOtp(Context);

        cut.Root.TagName.ShouldBe("div");
        cut.Root.ShouldHaveClass("md ion-input-otp");
        cut.Root.ShouldHaveClass("input-otp-size-medium");
        cut.Root.ShouldHaveClass("input-otp-shape-round");
        cut.Root.ShouldHaveClass("input-otp-fill-outline");

        cut.FindByClass("input-otp-group").ShouldHaveSingleItem();
        cut.FindByClass("native-wrapper").Count.ShouldBe(4);
        var inputs = cut.FindByClass("native-input").OfType<InputElement>().ToList();
        inputs.Count.ShouldBe(4);
        inputs.ShouldAllBe(input => input.Type == InputType.Text);
        inputs.Select(input => input.Value).ShouldAllBe(value => value == string.Empty);
        inputs.Select(input => input.Id).Distinct().Count().ShouldBe(4);

        cut.FindByClass("input-otp-description").Single()
            .ShouldNotHaveClass("input-otp-description-hidden");
        cut.GetTextContent().ShouldContain("Didn't get a code? Resend the code");
    }

    [Fact]
    public void IonInputOtp_LengthValueAndSeparatorsShapeTheInputs()
    {
        var cut = RenderOtp(Context, parameters =>
        {
            parameters.Add(nameof(IonInputOtp.Length), 6);
            parameters.Add(nameof(IonInputOtp.Value), "12x34567");
            parameters.Add(nameof(IonInputOtp.Separators), "2,4,9");
        });

        var values = cut.FindByClass("native-input")
            .OfType<InputElement>()
            .Select(input => input.Value)
            .ToList();
        values.ShouldBe(new string?[] { "1", "2", "3", "4", "5", "6" });
        cut.FindByClass("input-otp-separator").Count.ShouldBe(2);
    }

    [Fact]
    public void IonInputOtp_AllSeparatorsRendersBetweenEveryBox()
    {
        var cut = RenderOtp(Context, parameters =>
        {
            parameters.Add(nameof(IonInputOtp.Length), 6);
            parameters.Add(nameof(IonInputOtp.Separators), "all");
        });

        cut.FindByClass("input-otp-separator").Count.ShouldBe(5);
    }

    [Fact]
    public void IonInputOtp_EmptyDescriptionIsHidden()
    {
        var cut = RenderOtp(Context, withDescription: false);

        cut.FindByClass("input-otp-description").Single()
            .ShouldHaveClass("input-otp-description-hidden");
    }

    [Fact]
    public void IonInputOtp_StampsStateAndVariantClasses()
    {
        UsePlatform(HostPlatform.Ios);
        var cut = RenderOtp(Context, parameters =>
        {
            parameters.Add(nameof(IonInputOtp.Disabled), true);
            parameters.Add(nameof(IonInputOtp.Readonly), true);
            parameters.Add(nameof(IonInputOtp.Fill), "solid");
            parameters.Add(nameof(IonInputOtp.Shape), "soft");
            parameters.Add(nameof(IonInputOtp.Size), "large");
            parameters.Add(nameof(IonInputOtp.Color), "danger");
        });

        cut.Root.ShouldHaveClass("ios ion-input-otp");
        cut.Root.ShouldHaveClass("input-otp-disabled");
        cut.Root.ShouldHaveClass("input-otp-readonly");
        cut.Root.ShouldHaveClass("input-otp-fill-solid");
        cut.Root.ShouldHaveClass("input-otp-shape-soft");
        cut.Root.ShouldHaveClass("input-otp-size-large");
        cut.Root.ShouldHaveClass("ion-color-danger");
        cut.FindByClass("native-input").OfType<InputElement>()
            .ShouldAllBe(input => input.HasState(ElementState.Disabled));
    }

    [Fact]
    public void IonInputOtp_StylesApplySizeShapeAndModeBorder()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        var cut = RenderOtp(Context, parameters =>
        {
            parameters.Add(nameof(IonInputOtp.Size), "large");
            parameters.Add(nameof(IonInputOtp.Shape), "rectangular");
        });
        var input = cut.FindByClass("native-input").First();
        var style = cut.GetComputedStyle(input)!;
        var box = cut.GetBoxModel(input)!;

        box.BorderBox.Width.ShouldBe(56f, 0.5f);
        box.BorderBox.Height.ShouldBe(56f, 0.5f);
        style.BorderTopWidth.ShouldBe(Length.Px(1));
        style.BorderTopLeftRadius.ShouldBe(Length.Px(0));
    }
}

public class IonInputOtpInteractionTests : IDisposable
{
    private const float Width = 390;
    private const float Height = 240;
    private readonly SKBitmap _bitmap = new((int)Width, (int)Height);
    private readonly SKCanvas _canvas;

    public IonInputOtpInteractionTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void TypingValidCharactersAdvancesFocusAndCompletes()
    {
        var (app, page) = BuildApp();
        Focus(app, 0);

        app.Controller.OnTextInput("1");
        app.Engine.Render(_canvas);
        CurrentInputs(app).Select(input => input.HasState(ElementState.Focus))
            .ShouldBe(new[] { false, true, false, false });
        app.Controller.OnTextInput("2");
        app.Controller.OnTextInput("3");
        app.Controller.OnTextInput("4");

        page.Value.ShouldBe("1234");
        page.Inputs.ShouldBe(new[] { "1", "12", "123", "1234" });
        page.Completions.ShouldBe(new[] { "1234" });
        CurrentInputs(app).Select(input => input.Value)
            .ShouldBe(new string?[] { "1", "2", "3", "4" });
    }

    [Fact]
    public void InvalidCharacterIsRejectedWithoutEmitting()
    {
        var (app, page) = BuildApp();
        Focus(app, 0);

        app.Controller.OnTextInput("x");

        page.Value.ShouldBe(string.Empty);
        page.Inputs.ShouldBeEmpty();
        CurrentInputs(app)[0].Value.ShouldBe(string.Empty);
        CurrentInputs(app)[0].HasState(ElementState.Focus).ShouldBeTrue();
    }

    [Fact]
    public void ComposedInputIsDistributedAcrossBoxes()
    {
        var (app, page) = BuildApp();
        Focus(app, 0);

        app.Controller.OnTextInput("9876");

        page.Value.ShouldBe("9876");
        page.Inputs.ShouldBe(new[] { "9876" });
        page.Completions.ShouldBe(new[] { "9876" });
        CurrentInputs(app).Select(input => input.Value)
            .ShouldBe(new string?[] { "9", "8", "7", "6" });
    }

    [Fact]
    public void BackspaceShiftsFollowingValuesOnlyOnce()
    {
        var (app, page) = BuildApp(host => host.Value = "1234");
        Focus(app, 1);

        var prevented = app.Controller.OnKeyDown(MikoKey.Backspace, MikoKeyModifiers.None);

        prevented.ShouldBeTrue();
        page.Value.ShouldBe("134");
        CurrentInputs(app).Select(input => input.Value)
            .ShouldBe(new string?[] { "1", "3", "4", string.Empty });
    }

    [Fact]
    public void BlurAfterEditingRaisesChangeButInternalFocusDoesNot()
    {
        var (app, page) = BuildApp();
        Focus(app, 0);

        app.Controller.OnTextInput("1");
        page.Changes.ShouldBeEmpty();
        page.Blurs.ShouldBe(0);

        app.Controller.SetFocus(null);

        page.Blurs.ShouldBe(1);
        page.Changes.ShouldBe(new[] { "1" });
    }

    [Fact]
    public void ReadonlyRestoresTextAndDoesNotEmit()
    {
        var (app, page) = BuildApp(host =>
        {
            host.Value = "12";
            host.Readonly = true;
        });
        Focus(app, 1);

        app.Controller.OnTextInput("9");

        page.Value.ShouldBe("12");
        page.Inputs.ShouldBeEmpty();
        CurrentInputs(app).Select(input => input.Value)
            .ShouldBe(new string?[] { "1", "2", string.Empty, string.Empty });
    }

    private (MikoAppContext app, HostPage page) BuildApp(Action<HostPage>? configure = null)
    {
        var page = new HostPage();
        configure?.Invoke(page);
        var builder = MikoAppBuilder.CreateDefault();
        builder.AddIonic(options => options.Platform = HostPlatform.Android);
        builder.UseRootComponent(page.Build);
        var app = builder.Build();
        app.Controller.Initialize(_canvas, Width, Height);
        app.Engine.Render(_canvas);
        return (app, page);
    }

    private static List<InputElement> CurrentInputs(MikoAppContext app)
        => app.Engine.GetRoot()!.FindByClass("native-input").OfType<InputElement>().ToList();

    private static void Focus(MikoAppContext app, int index)
        => app.Controller.SetFocus(CurrentInputs(app)[index]);

    private sealed class HostPage : ComponentBase
    {
        public string? Value { get; set; } = string.Empty;
        public bool Readonly { get; set; }
        public List<string?> Inputs { get; } = new();
        public List<string?> Changes { get; } = new();
        public List<string> Completions { get; } = new();
        public int Blurs { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<IonInputOtp>(0);
            builder.AddComponentParameter(1, nameof(IonInputOtp.Value), Value);
            builder.AddComponentParameter(2, nameof(IonInputOtp.Readonly), Readonly);
            builder.AddComponentParameter(3, nameof(IonInputOtp.ValueChanged),
                EventCallback.Factory.Create<string?>(this, value => Value = value));
            builder.AddComponentParameter(4, nameof(IonInputOtp.OnInput),
                EventCallback.Factory.Create<string?>(this, value => Inputs.Add(value)));
            builder.AddComponentParameter(5, nameof(IonInputOtp.OnChange),
                EventCallback.Factory.Create<string?>(this, value => Changes.Add(value)));
            builder.AddComponentParameter(6, nameof(IonInputOtp.OnComplete),
                EventCallback.Factory.Create<string>(this, value => Completions.Add(value)));
            builder.AddComponentParameter(7, nameof(IonInputOtp.OnBlur),
                EventCallback.Factory.Create(this, () => Blurs++));
            builder.CloseComponent();
        }
    }
}
