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

namespace linerider
{
    public static class EntryPoint
    {
        #region Methods

        [STAThread]
        public static void Main()
        {
            Program.Run();
        }

        #endregion Methods
    }

    public static class Program
    {
        #region Fields
        public static string BinariesFolder = "bin";
        public readonly static CultureInfo Culture = new CultureInfo("en-US");
        public static readonly string WindowTitle = "Line Rider: Advanced 1.01";
        public static Random Random;
        private static bool _crashed;
        private static GLWindow glGame;
        private static string _currdir;

        #endregion Fields

        #region Properties
        /// <summary>
        /// Gets the current directory. Ends in Path.DirectorySeperator
        /// </summary>
        public static string CurrentDirectory
        {
            get
            {
                if (_currdir == null)
                    _currdir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + "LRA" + Path.DirectorySeparatorChar;
                return _currdir;
            }
        }
        #endregion Properties

        #region Methods

        public static void Crash(Exception ex)
        {
            if (!_crashed)
            {
                _crashed = true;
                glGame.Track.BackupTrack();
            }
        }

        public static void NonFatalError(string err)
        {
            System.Windows.Forms.MessageBox.Show("Non Fatal Error: " + err);
        }

        public static void Run()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Settings.Load();
            Random = new Random();
            GameResources.Init();

            using (Toolkit.Init(new ToolkitOptions { EnableHighResolution = true, Backend = PlatformBackend.Default }))
            {
                using (glGame = new GLWindow())
                {
                    glGame.RenderSize = new System.Drawing.Size(1280, 720);
                    Drawing.GameRenderer.Game = glGame;
                    var ms = new MemoryStream(GameResources.icon);
                    glGame.Icon = new System.Drawing.Icon(ms);

                    ms.Dispose();
                    glGame.Title = WindowTitle;
                    glGame.Run(60, 0);
                }
                Audio.AudioPlayback.CloseDevice();
            }
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Crash((Exception)e.ExceptionObject);
            if (System.Windows.Forms.MessageBox.Show("Unhandled Exception: " + e.ExceptionObject + "\r\n\r\nWould you like to export the crash data to a log.txt?", "Error!", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                if (!File.Exists(CurrentDirectory + "log.txt"))
                    File.Create(CurrentDirectory + "log.txt").Dispose();

                string append = WindowTitle + "\r\n" + e.ExceptionObject.ToString() + "\r\n";
                string begin = File.ReadAllText(CurrentDirectory + "log.txt", System.Text.Encoding.ASCII);
                File.WriteAllText(CurrentDirectory + "log.txt", begin + append, System.Text.Encoding.ASCII);
            }

            throw (Exception)e.ExceptionObject;
        }

        #endregion Methods
    }
}