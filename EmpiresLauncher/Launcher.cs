using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace EmpiresLauncher
{
    public partial class Launcher : Form
    {
        private const string sourceSdkBase2007Name = "source sdk base 2007";

        private const string hl2ExeName = "hl2.exe";

        private const string empiresName = "empires";

        private const string steamappsName = "steamapps";

        private const string installSourceSdkBase2007Uri = "steam://run/218";

        private const string gameLoadedGuid = "{0C4BCE33-258D-4189-AE0D-B217820B7C2C}";

        private const string steamPathRegistryKey = @"HKEY_CURRENT_USER\Software\Valve\Steam";

        private const string steamPathRegistryValue = "SteamPath";

        public Launcher()
        {
            InitializeComponent();
            Show();
            BringToFront();

            var argsLine = string.Join(" ", Environment.GetCommandLineArgs());

            RunEmpires(argsLine);
        }

        private void RunEmpires(string argsLine)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var steamappsDirectory = Directory.GetParent(currentDirectory).Parent.FullName;
            var sourceSdkBase2007Directory = FindGameDirectory(sourceSdkBase2007Name, steamappsDirectory, hl2ExeName);
            var sourceSdkBase2007Exists = !string.IsNullOrEmpty(sourceSdkBase2007Directory);

            if (sourceSdkBase2007Exists)
            {
                RunEmpiresAsMod(argsLine, sourceSdkBase2007Directory);
            }
            else
            {
                // Search for Source SDK Base 2007 in Steam path
                sourceSdkBase2007Directory = FindGameDirectoryInSteamPath(sourceSdkBase2007Name, hl2ExeName);
                sourceSdkBase2007Exists = !string.IsNullOrEmpty(sourceSdkBase2007Directory);

                if (sourceSdkBase2007Exists)
                {
                    RunEmpiresAsMod(argsLine, sourceSdkBase2007Directory);
                }
                else
                {
                    var result = MessageBox.Show("Can't start Empires because Source SDK Base 2007 was not found.\n\nClick OK to install and run Source SDK Base 2007. After Source SDK Base 2007 has run, quit it, and start Empires again.", "Empires Mod", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    if (result == DialogResult.OK)
                    {
                        using (var process = Process.Start(installSourceSdkBase2007Uri))
                        {
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        Environment.Exit(0);
                    }
                }
            }
        }

        private static void RunEmpiresAsMod(string argsLine, string sourceSdkBase2007Directory)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var empiresModDirectory = Path.Combine(currentDirectory, empiresName);
            var launchArguments = string.Format("-game \"{0}\" {1}", empiresModDirectory, argsLine);
            var sourceSdkBase2007Hl2Exe = Path.Combine(sourceSdkBase2007Directory, hl2ExeName);
            var startInfo = new ProcessStartInfo()
            {
                FileName = sourceSdkBase2007Hl2Exe,
                Arguments = launchArguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };

            using (var hl2Process = new Process() { StartInfo = startInfo, EnableRaisingEvents = true })
            {
                // Close the launcher once the game has loaded
                hl2Process.OutputDataReceived += hl2Process_Exited;

                try
                {
                    hl2Process.Start();
                }
                catch (Win32Exception)
                {
                    MessageBox.Show("Can't start Empires because there was a problem running the game.", "Empires Mod", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // Re-throw exception so crash dump is generated
                    throw;
                }

                hl2Process.BeginOutputReadLine();
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

        private static string FindGameDirectoryInSteamPath(string targetGameName, string requiredFile)
        {
            var steamPath = (string)Registry.GetValue(steamPathRegistryKey, steamPathRegistryValue, null);
            var steamPathExists = !string.IsNullOrEmpty(steamPath);
            var steamPathIsValid = steamPathExists && Directory.Exists(steamPath);

            if (steamPathIsValid)
            {
                var steamappsDirectory = Path.Combine(steamPath, steamappsName);
                var steamappsDirectoryExists = Directory.Exists(steamappsDirectory);

                if (steamappsDirectoryExists)
                {
                    return FindGameDirectory(targetGameName, steamappsDirectory, requiredFile);
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

        private static void hl2Process_Exited(object sender, DataReceivedEventArgs e)
        {
            // Data may be null if game exits unexpectedly
            var dataExists = e.Data != null;

            if (dataExists)
            {
                var dataContainsGameLoadedGuid = dataExists && e.Data.Contains(gameLoadedGuid);

                if (dataContainsGameLoadedGuid)
                {
                    Application.Exit();
                }
                else
                {
                    // Current output doesn't contain the game loaded GUID flag. Ignore and handle next output
                }
            }
            else
            {
                // Close launcher because game exited unexpectedly
                Application.Exit();
            }
        }
    }
}
