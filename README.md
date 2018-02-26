# About
An open source spiritual successor to the flash game Line Rider 6.2 with lots of new features.

# Instructions
You can download the latest version [on the releases page](https://github.com/jealouscloud/linerider-advanced/releases)

## Windows
If you can't run the application, you probably need to install [.net 4.6](https://www.microsoft.com/en-us/download/details.aspx?id=48130) which is a requirement for running LRA.
## Mac/Linux
You will need the [mono framework](http://www.mono-project.com/download/stable/) installed in order to run LRA.

# Features
* Fast
* Backwards compatible physics
* Cross platform

There's lots and lots of new features, but I intend to list those in the wiki, eventually.

# Issues
Be sure to post an issue you've found in the issue tracker https://github.com/jealouscloud/linerider-advanced/issues

# Build
Run nuget restore in src (Visual Studio will do this for you)
Build src/linerider.sln with msbuild or Visual Studio

This project requires .net 4.6 and C# 7 support.

# Libraries
This project uses binaries, sources, or modified sources from the following libraries:

* ffmpeg https://ffmpeg.org/
* NVorbis https://github.com/ioctlLR/NVorbis
* gwen-dotnet https://code.google.com/archive/p/gwen-dotnet/
* OpenTK https://github.com/opentk/opentk

You can find their license info in LICENSES.txt

The UI is a personally modified version of gwen-dotnet

# License
linerider-advanced is licensed under GPL3.