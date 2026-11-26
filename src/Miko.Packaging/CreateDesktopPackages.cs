using Microsoft.Build.Framework;

namespace Miko.Packaging;

public sealed class CreateDesktopPackages : Microsoft.Build.Utilities.Task
{
    [Required] public string PublishDirectory { get; set; } = "";
    [Required] public string ProjectDirectory { get; set; } = "";
    [Required] public string RuntimeIdentifier { get; set; } = "";
    [Required] public string AppName { get; set; } = "";
    [Required] public string Version { get; set; } = "";
    public string OutputDirectory { get; set; } = "";
    public string Formats { get; set; } = "";
    public string PackageId { get; set; } = "";
    public string License { get; set; } = "";
    public string Maintainer { get; set; } = "";
    public string Description { get; set; } = "";
    public string DebDepends { get; set; } = "";
    public string[] RpmRequires { get; set; } = [];
    [Output] public string[] Packages { get; private set; } = [];

    public override bool Execute()
    {
        try
        {
            var source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(PublishDirectory, ProjectDirectory));
            var output = string.IsNullOrEmpty(OutputDirectory)
                ? Path.Combine(Path.GetDirectoryName(source)!, "packages")
                : Path.GetFullPath(OutputDirectory, ProjectDirectory);
            var package = new DesktopPackage(source, AppName, Version, RuntimeIdentifier, Maintainer, Description, PackageId, License)
            {
                DebDepends = DebDepends,
                RpmRequires = RpmRequires
            };
            Packages = package.Build(output, Formats);
            foreach (var path in Packages)
                Log.LogMessage(MessageImportance.High, "Miko.Packaging -> {0}", path);
            return true;
        }
        catch (Exception e)
        {
            Log.LogErrorFromException(e);
            return false;
        }
    }
}
