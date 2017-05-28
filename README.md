# linerider-advanced
An open source spiritual successor to the flash game Line Rider 6.2

# foreword
This source is awful. It's bad, and I should feel bad. I do not intend to repair it. Occasional feature requests, bug fixes, etc are fine, but I do not have the will to put in the effort to make this the code base I know it should be.
There are quite a few things put down on day of release (May 27 2017) that I almost instantly intend to change, but I wanted a release by today.
# issues
Be sure to post an issue you've found in the issue tracker https://github.com/jealouscloud/linerider-advanced/issues

# build
in order to build, open up your favorite terminal or command prompt on windows and cd to the 'lib' directory.
run the appropriate script per your operating system:
linux: build.sh
osx: osxbuild.sh
windows: build.ps1

If you don't know how to run a powershell (windows) script/shell script (osx), I'm sure google can help you.

Once you've done that you can open up linerider.sln in your C# ide of choice, build it against .net 4.0 client profile, and you should have a running version.
All files are designed to output to 'build'.

# license
linerider-advanced is licensed under GPL3.

# libraries
This program features code from the following libraries. Their license information can be found in LICENSES.txt:
nanosvg https://github.com/memononen/nanosvg

Microsoft Public License

NVorbis https://github.com/ioctlLR/NVorbis

MIT License

gwen-dotnet https://code.google.com/archive/p/gwen-dotnet/
QuickFont https://github.com/opcon/QuickFont
NGraphics https://github.com/praeclarum/NGraphics


License: http://oss.sgi.com/projects/FreeB/
LibTessDotNet https://github.com/speps/LibTessDotNet

BSD 2-clause "Simplified" License
agg-sharp https://github.com/MatterHackers/agg-sharp
