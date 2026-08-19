using Miko.Components;

namespace Miko.Ionic.Components;

internal sealed record IonOverlayEntry(
    string Id,
    long Version,
    RenderFragment Content,
    bool ControllerOwned);
