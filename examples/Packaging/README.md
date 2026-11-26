# Package the desktop sample

Run from the repository root with the .NET 11 SDK. The sample uses the shared MikoAppBlank UI and consumes Miko.Packaging from the repository's local NuGet feed.

```sh
dotnet pack src/Miko.Packaging -c Release
dotnet publish examples/Packaging -c Release -r win-x64
dotnet publish examples/Packaging -c Release -r linux-x64
dotnet publish examples/Packaging -c Release -r osx-x64
```

Each command writes its installers and archives to `examples/Packaging/bin/Release/net11.0/<RID>/packages/`. The sample selects all formats for the three target platforms. To generate just the RPM, run:

```sh
dotnet publish examples/Packaging -c Release -r linux-x64 -p:MikoPackageFormats=rpm
```
