
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");
var buildDir = Directory("./dependencies");

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetInstallFromConfig("./packages.config", new NuGetInstallSettings {
    OutputDirectory="./dependencies"
    });
    CopyFiles(new string[] { "./dependencies/OpenTK.2.0.0/lib/net20/OpenTK.dll",
    "./dependencies/OpenTK.2.0.0/content/OpenTK.dll.config"}, "./dependencies");
});

Task("Build")
  .IsDependentOn("Restore-NuGet-Packages")
  .Does(() =>
{

    DotNetBuild("./Agg/Agg.csproj", settings => settings.SetConfiguration("Debug"));
    DotNetBuild("./Gwen/Gwen.csproj", settings => settings.SetConfiguration("Debug"));
    DotNetBuild("./QuickFont/QuickFont.csproj", settings => settings.SetConfiguration("Debug"));
    DotNetBuild("./Gwen.Renderer.OpenTK/Gwen.Renderer.OpenTK.csproj", settings => settings.SetConfiguration("Debug"));
    DotNetBuild("./NVorbis/NVorbis.csproj", settings => settings.SetConfiguration("Debug"));
    DotNetBuild("./LibTessDotNet/LibTessDotNet.csproj", settings => settings.SetConfiguration("Debug"));
});
Task("Default")
  .IsDependentOn("Build")
  .Does(() =>
{

    var files = GetFiles("./dependencies/*.dll");
    CopyFiles(files,"../build");
    files = GetFiles("./dependencies/*.pdb");
    CopyFiles(files,"../build");
    Information("Build complete.");
});

RunTarget(target);