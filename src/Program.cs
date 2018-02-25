//
//  Program.cs
//
//  Author:
//       Noah Ablaseau <nablaseau@hotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using OpenTK;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

namespace linerider
{
    public static class Program
    {
#if DEBUG
        public static bool IsDebugged = false;
        public static bool LogGL => IsDebugged;
#endif
        public static string BinariesFolder = "bin";
        public readonly static CultureInfo Culture = new CultureInfo("en-US");
        public static string Version = "1.03a";
        public static string TestVersion = "1.03 unstable a";
        public static string NewVersion = null;
        public static readonly string WindowTitle = "Line Rider: Advanced " + TestVersion;
        public static Random Random;
        private static bool _crashed;
        private static MainWindow glGame;
        private static string _currdir;
        private static string _userdir;

        /// <summary>
        /// Gets the current directory. Ends in Path.DirectorySeperator
        /// </summary>
        public static string UserDirectory
        {
            get
            {
                if (_userdir == null)
                {
                    _userdir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    //mono doesnt do well with non windows ~/Documents.
                    if (_userdir == Environment.GetFolderPath(Environment.SpecialFolder.Personal))
                    {
                        string documents = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Documents");
                        //so, if we can find a Documents folder, we use that.
                        //otherwise we're just gonna use ~/LRA, unfortunately.
                        if (Directory.Exists(documents))
                        {
                            _userdir = documents;
                        }
                    }
                    _userdir += Path.DirectorySeparatorChar + "LRA" + Path.DirectorySeparatorChar;
                }
                return _userdir;
            }
        }
        public static string CurrentDirectory
        {
            get
            {
                if (_currdir == null)
                    _currdir = AppDomain.CurrentDomain.BaseDirectory;
                return _currdir;
            }
        }

        public static void Crash(Exception e)
        {
            if (!_crashed)
            {
                _crashed = true;
                glGame.Track.BackupTrack();
            }
            if (System.Windows.Forms.MessageBox.Show(
                "Unhandled Exception: " +
                e.Message +
                Environment.NewLine +
                e.StackTrace +
                Environment.NewLine +
                "Would you like to export the crash data to a log.txt?",
                "Error!",
                System.Windows.Forms.MessageBoxButtons.YesNo)
                 == System.Windows.Forms.DialogResult.Yes)
            {
                if (!File.Exists(UserDirectory + "log.txt"))
                    File.Create(UserDirectory + "log.txt").Dispose();

                string append = WindowTitle + "\r\n" + e.ToString() + "\r\n";
                string begin = File.ReadAllText(UserDirectory + "log.txt", System.Text.Encoding.ASCII);
                File.WriteAllText(UserDirectory + "log.txt", begin + append, System.Text.Encoding.ASCII);
            }
            throw e;
        }

        public static void NonFatalError(string err)
        {
            System.Windows.Forms.MessageBox.Show("Non Fatal Error: " + err);
        }
        public static void Run()
        {
#if DEBUG
            if (IsDebugged)
            {
                Debug.Listeners.Add(new TextWriterTraceListener(System.Console.Out));
            }
            else
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            }
#endif
            if (!Directory.Exists(UserDirectory))
            {
                Directory.CreateDirectory(UserDirectory);
                System.Windows.Forms.MessageBox.Show("LRA User directory created at:\r\n" + UserDirectory);
            }
            Settings.Load();

            if (!Directory.Exists(UserDirectory + "Songs"))
                Directory.CreateDirectory(UserDirectory + "Songs");
            if (!Directory.Exists(UserDirectory + "Tracks"))
                Directory.CreateDirectory(UserDirectory + "Tracks");

            Random = new Random();
            GameResources.Init();

            using (Toolkit.Init(new ToolkitOptions { EnableHighResolution = true, Backend = PlatformBackend.Default }))
            {
                using (glGame = new MainWindow())
                {
                    glGame.RenderSize = new System.Drawing.Size(1280, 720);
                    Rendering.GameRenderer.Game = glGame;
                    var ms = new MemoryStream(GameResources.icon);
                    glGame.Icon = new System.Drawing.Icon(ms);

                    ms.Dispose();
                    glGame.Title = WindowTitle;
                    glGame.Run(60, 0);//todo maybe not limit this
                }
                Audio.AudioService.CloseDevice();
            }
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Crash((Exception)e.ExceptionObject);

            throw (Exception)e.ExceptionObject;
        }
        public static void UpdateCheck()
        {
            if (Settings.CheckForUpdates)
            {
                new System.Threading.Thread(() =>
                {
                    try
                    {
                        using (WebClient wc = new WebClient())
                        {
                            string currentversion = wc.DownloadString("https://raw.githubusercontent.com/jealouscloud/linerider-advanced/develop/version");
                            var idx = currentversion.IndexOfAny(new char[] { '\r', '\n' });
                            if (idx != -1)
                            {
                                currentversion = currentversion.Remove(idx);
                            }
                            if (currentversion != Version && currentversion.Length > 0)
                            {
                                NewVersion = currentversion;
                            }
                        }
                    }
                    catch
                    {
                    }
                })
                {
                    IsBackground = true
                }.Start();
            }
        }
    }
}
