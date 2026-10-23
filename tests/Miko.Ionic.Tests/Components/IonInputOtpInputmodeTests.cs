using Miko.Ionic.Tests.Fixtures;
using Miko.Ionic.Tests.Fixtures.Routed;
using Miko.Platform;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// <see cref="Miko.Ionic.Components.IonInputOtp.Inputmode"/> (P0-C1) picks the soft keyboard: a
/// tapped box publishes <see cref="InputMethodType.Number"/> (the platform number pad) or
/// <see cref="InputMethodType.Text"/> to the host's input method. The parameter used to be ignored,
/// and every box asked for the text keyboard — even <c>Type="number"</c>, Ionic's numeric default.
/// </summary>
public sealed class IonInputOtpInputmodeTests : IDisposable
{
    private readonly RoutedApp _app = new(("/", typeof(OtpPageFixture)));
    private readonly RecordingInputMethod _inputMethod = new();

    public IonInputOtpInputmodeTests() => _app.App.Controller.AttachInputMethod(_inputMethod);

    public void Dispose() => _app.Dispose();

    [Theory]
    [InlineData("otp-default", InputMethodType.Number)]
    [InlineData("otp-text", InputMethodType.Text)]
    [InlineData("otp-text-numeric", InputMethodType.Number)]
    [InlineData("otp-number-text", InputMethodType.Text)]
    public void Should_ask_for_the_keyboard_inputmode_selects(string otp, InputMethodType keyboard)
    {
        _app.Tap(_app.Single(otp).FindByClass("native-input").First());
        _app.Render();

        _inputMethod.State.ShouldNotBeNull("the tapped box is the input method's client").InputType.ShouldBe(keyboard);
    }

    private sealed class RecordingInputMethod : InputMethodBase
    {
        public InputMethodState? State { get; private set; }

        public override void SetState(InputMethodState? state) => State = state;
    }
}
