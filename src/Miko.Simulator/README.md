# Miko.Simulator

Device simulator host for [Miko](https://github.com/chaldea/miko).

`Miko.Simulator` runs a Miko application inside a desktop window that emulates a
mobile device. The window is split in two:

- **Left** — the application, rendered into its own independent canvas sized to
  the simulated device's logical resolution (so the app sees the same viewport
  it would on a real phone), drawn inside a device bezel.
- **Right** — a settings panel (device picker, orientation, safe-area toggle)
  that is itself built and rendered with the Miko engine.

## Usage

A simulator startup project sits alongside the other platform heads:

```
MyApp
├── MyApp            # shared app (routes, layout, styles)
├── MyApp.Simulator  # this project
├── MyApp.Desktop
├── MyApp.iOS
└── MyApp.Android
```

`Program.cs`:

```csharp
using Miko.Simulator;
using MyApp;

var context = App.CreateContext();
context.RunSimulator();
```

Configure the device list, initial device, or orientation:

```csharp
context.RunSimulator(o =>
{
    o.InitialDevice = DeviceProfile.IPhoneSE;
    o.InitialOrientation = Orientation.Landscape;
    o.Devices = new[] { DeviceProfile.IPhone15Pro, DeviceProfile.Pixel7 };
});
```

The simulator drives the same platform-agnostic `MikoInteractionController` the
real device hosts use, so pointer, scroll, keyboard, and safe-area behavior match
what the app gets on-device.
