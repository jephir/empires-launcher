using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace EmpiresLauncher
{
    public partial class Launcher : Form
    {
        private const string sourceSdkBase2007Name = "source sdk base 2007";

        private const string sourceSdkBase2007DriveNotice = "Source SDK Base 2007 must be installed on the same drive as Empires. If it's not, remove Empires using Steam and install it again to the same drive as Source SDK Base 2007.";

        private const string hl2ExeName = "hl2.exe";

        private const string empiresName = "empires";

        private const string installSourceSdkBase2007Uri = "steam://run/218";

        private const string gameLoadedGuid = "{0C4BCE33-258D-4189-AE0D-B217820B7C2C}";

        public Launcher()
        {
            InitializeComponent();
            Show();
            BringToFront();
            RunEmpires(String.Join(" ", Environment.GetCommandLineArgs()));
        }

        private void RunEmpires(string args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var steamappsDirectory = Directory.GetParent(currentDirectory).Parent.FullName;
            var sourceSdkBase2007Directory = FindGameDirectory(sourceSdkBase2007Name, steamappsDirectory, hl2ExeName);
            var sourceSdkBase2007Exists = !String.IsNullOrEmpty(sourceSdkBase2007Directory);
            
            if (sourceSdkBase2007Exists)
            {
                var sourceSdkBase2007Hl2Exe = Path.Combine(sourceSdkBase2007Directory, hl2ExeName);
                var empiresModDirectory = Path.Combine(currentDirectory, empiresName);
                var launchArguments = String.Format("-game \"{0}\" {1}", empiresModDirectory, args);
                var startInfo = new ProcessStartInfo()
                {
                    FileName = sourceSdkBase2007Hl2Exe,
                    Arguments = launchArguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                };

                using (var process = new Process() { StartInfo = startInfo })
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data.Contains(gameLoadedGuid))
                        {
                            Application.Exit();
                        }
                    };

                    try
                    {
                        process.Start();
                    }
                    catch (Win32Exception)
                    {
                        MessageBox.Show("Can't start Empires because there was a problem running the game.", "Empires Mod", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        throw;
                    }

                    process.BeginOutputReadLine();
                }
            }
            else
            {
                var result = MessageBox.Show("Can't start Empires because Source SDK Base 2007 was not found.\n\nClick OK to install and run Source SDK Base 2007. After Source SDK Base 2007 has run, quit it, and start Empires again.\n\n" + sourceSdkBase2007DriveNotice, "Empires Mod", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                if (result == DialogResult.OK)
                {
                    using (var process = Process.Start(installSourceSdkBase2007Uri))
                    {
                        Environment.Exit(0);
                    }
                }
            }
        }

        private static string FindGameDirectory(string targetGameName, string searchRoot, string requiredFile)
        {
            var steamappDirectories = Directory.GetDirectories(searchRoot);

            foreach (var steamappDirectory in steamappDirectories)
            {
                var gameDirectories = Directory.GetDirectories(steamappDirectory);

                foreach (var gameDirectory in gameDirectories)
                {
                    var gameDirectoryName = Path.GetFileName(gameDirectory);
                    var gameDirectoryEqualsTarget = gameDirectoryName.Equals(targetGameName, StringComparison.OrdinalIgnoreCase);
                    var gameDirectoryHasRequiredFile = gameDirectoryEqualsTarget && DirectoryContainsFileName(gameDirectory, requiredFile);

                    if (gameDirectoryHasRequiredFile)
                    {
                        return gameDirectory;
                    }
                }
            }

            return null;
        }

        private static bool DirectoryContainsFileName(string path, string fileName)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                if (Path.GetFileName(file).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
