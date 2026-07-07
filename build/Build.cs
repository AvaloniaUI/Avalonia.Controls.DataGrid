using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using NukeExtensions;

// Packaging in this repository is plain `dotnet pack` driven by azure-pipelines.yml; this
// minimal nuke build exists only to run the shared SBOM generation from build-common over
// the packed output. Each package's version is read from its own .nuspec, so no version
// computation is duplicated here.
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.CreateSbom);

    [NuGetPackage("CycloneDX", "CycloneDX.dll", Framework = "net10.0")] readonly Tool CycloneDx = null!;

    [Parameter("Directory containing the built .nupkg files to generate SBOMs for")]
    readonly AbsolutePath Packages = RootDirectory / "artifacts" / "package" / "release";

    [Parameter("Directory the generated SBOMs are written to")]
    readonly AbsolutePath Output = RootDirectory / "artifacts" / "sbom";

    // Generates a per-package CycloneDX SBOM for EU Cyber Resilience Act evidence and embeds
    // it into each .nupkg at _manifest/cyclonedx/bom.cdx.json, so it must run after packing
    // and before the packages are published.
    Target CreateSbom => _ => _
        .Executes(() => SbomGenerator.Generate(
            CycloneDx,
            RootDirectory,
            Packages,
            Output));
}
