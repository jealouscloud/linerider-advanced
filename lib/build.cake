var target = Argument("target", "Default");

Task("Clean")
    .Does(() =>
{
    CleanDirectories("./**/obj");
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetInstallFromConfig("./packages.config", new NuGetInstallSettings {
    OutputDirectory="./packages"
    });
    CopyFiles(new string[] { "./packages/OpenTK.2.0.0/lib/net20/OpenTK.dll",
    "./packages/OpenTK.2.0.0/content/OpenTK.dll.config"}, "../build");
});

Task("Build")
  .IsDependentOn("Restore-NuGet-Packages")
  .Does(() =>
{
    DotNetBuild("./linerider-dependencies.sln", settings => settings.SetConfiguration("Debug"));
});
Task("Default")
  .IsDependentOn("Build")
  .Does(() =>
{
    Information("Build complete.");
});

RunTarget(target);