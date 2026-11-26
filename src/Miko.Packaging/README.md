# Miko.Packaging

Reference this build-time package in a desktop executable targeting .NET 11. Publishing uses the target RID. A publish without an explicit RID uses the SDK host RID.

```xml
<PackageReference Include="Miko.Packaging" Version="1.0.0-dev" PrivateAssets="all" />
```

| Target RID | Default | Available formats |
| --- | --- | --- |
| win-x64, win-arm64 | msi | msi, zip |
| linux-x64, linux-arm64 | deb | deb, rpm, tar.gz |
| osx-x64, osx-arm64 | dmg | dmg, pkg |

Select formats in the application's project file. Command-line overrides can use an escaped separator, for example `-p:MikoPackageFormats=deb%3Brpm`.

```xml
<PropertyGroup Condition="'$(RuntimeIdentifier)' == 'linux-x64'">
  <MikoPackageFormats>deb;rpm;tar.gz</MikoPackageFormats>
  <MikoPackageId>org.example.myapp</MikoPackageId>
  <MikoDebDepends>libc6 (&gt;= 2.31)</MikoDebDepends>
  <MikoRpmRequires>glibc &gt;= 2.31</MikoRpmRequires>
</PropertyGroup>
```

Packages are named `<AssemblyName>-<Version>-<RID>.<format>` in the `packages` directory beside the publish directory. Set `MikoPackageOutputDirectory` to another directory outside the publish tree. Relative paths are resolved against the project directory. The packager includes the published apphost, dependencies and static assets. Framework-dependent applications require the matching .NET runtime on the destination. Use `--self-contained true` to include it.

MSI installs into Program Files. DEB and RPM install into `/opt/<MikoPackageId>`. PKG installs an application bundle under `/Applications`. DMG is a read-only disk image containing that installer. ZIP and tar.gz keep the publish layout. The project properties `Authors`, `Description` and `PackageLicenseExpression` supply package metadata. `MikoPackageId` defaults to a normalized assembly name.

`MikoDebDepends` accepts the Debian Depends field. `MikoRpmRequires` accepts semicolon-separated package names with optional version comparisons. Installers are generated unsigned. A signing target can run `AfterTargets="MikoPackageDesktop"` and consume the paths in `@(MikoDesktopPackage)`.

All writers run in managed code. The MSI writer uses OpenMcdf 3.4.0 for OLE compound-file storage, copyright 2010-2026 Federico Blaseotto and Jeremy Powell. The package contains its MPL-2.0 license. [OpenMcdf source](https://github.com/openmcdf/openmcdf)
