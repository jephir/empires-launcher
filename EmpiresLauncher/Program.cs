using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace EmpiresLauncher
{
    class Program
    {
        private const string sourceSdkBase2007Name = "source sdk base 2007";
        private const string sourceSdkBase2007DriveNotice = "Source SDK Base 2007 must be installed on the same drive as Empires. If it's not, remove Empires using Steam and install it again to the same drive as Source SDK Base 2007.";
        private const string hl2ExeName = "hl2.exe";
        private const string empiresName = "empires";
        private const string installSourceSdkBase2007Uri = "steam://run/218";

        static void Main(string[] args)
        {
            RunEmpires();
        }

        private static void RunEmpires()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var steamappsDirectory = Directory.GetParent(currentDirectory).Parent.FullName;
            var sourceSdkBase2007Directory = FindGameDirectory(sourceSdkBase2007Name, steamappsDirectory);
            var sourceSdkBase2007Exists = !String.IsNullOrEmpty(sourceSdkBase2007Directory);

            if (sourceSdkBase2007Exists)
            {
                var hl2ExeExists = DirectoryContainsFileName(sourceSdkBase2007Directory, hl2ExeName);

                if (hl2ExeExists)
                {
                    var sourceSdkBase2007Hl2Exe = Path.Combine(sourceSdkBase2007Directory, hl2ExeName);
                    var empiresModDirectory = Path.Combine(currentDirectory, empiresName);
                    var launchArguments = String.Format("-game \"{0}\"", empiresModDirectory);

                    var startInfo = new ProcessStartInfo()
                    {
                        FileName = sourceSdkBase2007Hl2Exe,
                        Arguments = launchArguments
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        process.WaitForExit();
                    }
                }
                else
                {
                    var result = MessageBox.Show("Can't start Empires because hl2.exe in Source SDK Base 2007 was not found. Click OK to run Source SDK Base 2007 and generate hl2.exe. After Source SDK Base 2007 has run, quit it, and start Empires again.\n\n" + sourceSdkBase2007DriveNotice, "Empires Mod", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    if (result == DialogResult.OK)
                    {
                        Process.Start(installSourceSdkBase2007Uri);
                    }                  
                }
            }
            else
            {
                var result = MessageBox.Show("Can't start Empires because Source SDK Base 2007 was not found. Click OK to install and run Source SDK Base 2007. After Source SDK Base 2007 has run, quit it, and start Empires again.\n\n" + sourceSdkBase2007DriveNotice, "Empires Mod", MessageBoxButtons.OK, MessageBoxIcon.Error);

                if (result == DialogResult.OK)
                {
                    Process.Start(installSourceSdkBase2007Uri);
                }
            }
        }

        private static string FindGameDirectory(string targetGameName, string searchRoot)
        {
            var steamappDirectories = Directory.GetDirectories(searchRoot);

            foreach (var steamappDirectory in steamappDirectories)
            {
                var gameDirectories = Directory.GetDirectories(steamappDirectory);

                foreach (var gameDirectory in gameDirectories)
                {
                    var gameDirectoryName = Path.GetFileName(gameDirectory);

                    if (gameDirectoryName.Equals(targetGameName, StringComparison.OrdinalIgnoreCase))
                    {
                        return gameDirectory;
                    }
                }
            }

            return null;
        }

        private static bool DirectoryContainsFileName(string directoryName, string fileName)
        {
            foreach (var file in Directory.GetFiles(directoryName))
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
