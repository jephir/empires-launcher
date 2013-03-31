using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace EmpiresLauncher
{
    public partial class Launcher : Form
    {
        private const string gameName = "empires";

        private const string sourceSdkBase2007Name = "source sdk base 2007";

        private const string steamappsName = "steamapps";

        private const string hl2ExeName = "hl2.exe";

        private const string installSourceSdkBase2007Uri = "steam://run/218";

        private const string steamPathRegistryKey = @"HKEY_CURRENT_USER\Software\Valve\Steam";

        private const string steamPathRegistryValue = "SteamPath";

        private const string gameLoadedMessage = "{0C4BCE33-258D-4189-AE0D-B217820B7C2C}";

        private static readonly string titleCaseGameName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(gameName);

        private static readonly string sourceSdkBase2007NotFoundErrorMessage = string.Format("Can't start {0} because Source SDK Base 2007 was not found.\n\nClick OK to install and run Source SDK Base 2007.", titleCaseGameName);

        private static readonly string hl2ProcessWin32ExceptionErrorMessage = string.Format("Can't start {0} because there was a problem running the game.", titleCaseGameName);

        public Launcher()
        {
            InitializeComponent();
            Show();
            BringToFront();

            // Pass arguments received by launcher to game
            // This allows hl2.exe options to be passed (e.g. -nosound)
            var launcherArguments = Environment.GetCommandLineArgs();
            var gameArguments = string.Join(" ", launcherArguments);

            PrepareToRunGame(gameName, gameArguments);
        }

        private static void PrepareToRunGame(string gameName, string gameArguments)
        {
            // Find the Source SDK Base 2007 directory
            var currentDirectory = Directory.GetCurrentDirectory();
            var steamappsDirectory = Directory.GetParent(currentDirectory).Parent.FullName;
            var sourceSdkBase2007Directory = FindApplicationDirectory(sourceSdkBase2007Name, steamappsDirectory, hl2ExeName);
            var sourceSdkBase2007Exists = !string.IsNullOrEmpty(sourceSdkBase2007Directory);

            if (sourceSdkBase2007Exists)
            {
                RunGameAsMod(gameName, gameArguments, sourceSdkBase2007Directory);
            }
            else
            {
                // Source SDK Base 2007 may not be found if installed on different drive from game
                // To solve this, search for Source SDK Base 2007 in Steam path from Windows registry
                sourceSdkBase2007Directory = FindApplicationDirectoryInSteamPath(sourceSdkBase2007Name, hl2ExeName);
                sourceSdkBase2007Exists = !string.IsNullOrEmpty(sourceSdkBase2007Directory);

                if (sourceSdkBase2007Exists)
                {
                    RunGameAsMod(gameName, gameArguments, sourceSdkBase2007Directory);
                }
                else
                {
                    // Source SDK Base 2007 not found - prompt user to install it
                    var result = MessageBox.Show(sourceSdkBase2007NotFoundErrorMessage, titleCaseGameName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    if (result == DialogResult.OK)
                    {
                        Process.Start(installSourceSdkBase2007Uri);
                    }
                    // Close launcher - Source SDK Base 2007 should be installing now
                    Environment.Exit(0);
                }
            }
        }

        private static void RunGameAsMod(string gameName, string argsLine, string sourceSdkBase2007Directory)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var modDirectory = Path.Combine(currentDirectory, gameName);
            var launchArguments = string.Format("-game \"{0}\" {1}", modDirectory, argsLine);
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
                hl2Process.OutputDataReceived += hl2Process_OutputDataReceived;

                try
                {
                    hl2Process.Start();
                }
                catch (Win32Exception)
                {
                    MessageBox.Show(hl2ProcessWin32ExceptionErrorMessage, titleCaseGameName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // Re-throw exception so crash dump is generated
                    throw;
                }

                hl2Process.BeginOutputReadLine();
            }
        }

        private static string FindApplicationDirectory(string applicationName, string steamappsDirectory, string requiredFile)
        {
            var usernameDirectories = Directory.GetDirectories(steamappsDirectory);

            foreach (var steamappDirectory in usernameDirectories)
            {
                var gameDirectories = Directory.GetDirectories(steamappDirectory);

                foreach (var gameDirectory in gameDirectories)
                {
                    var gameDirectoryName = Path.GetFileName(gameDirectory);
                    var gameDirectoryEqualsTarget = gameDirectoryName.Equals(applicationName, StringComparison.OrdinalIgnoreCase);
                    var gameDirectoryHasRequiredFile = gameDirectoryEqualsTarget && DirectoryContainsFileName(gameDirectory, requiredFile);

                    if (gameDirectoryHasRequiredFile)
                    {
                        return gameDirectory;
                    }
                }
            }

            return null;
        }

        private static string FindApplicationDirectoryInSteamPath(string applicationName, string requiredFile)
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
                    return FindApplicationDirectory(applicationName, steamappsDirectory, requiredFile);
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

        private static void hl2Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // Data may be null if game exits unexpectedly
            var dataExists = e.Data != null;

            if (dataExists)
            {
                // Add the std::cout code below to the client initialization of the mod to signal when the game has loaded

                /*
                 * CHLClient::CHLClient() 
                 * {
                 * // Guid print to standard output to notify the launcher that the game has started, and the splash screen can be closed
                 * std::cout << "{0C4BCE33-258D-4189-AE0D-B217820B7C2C}" << std::endl;
                 * ...
                 */
                var dataContainsGameLoadedMessage = dataExists && e.Data.Contains(gameLoadedMessage);

                if (dataContainsGameLoadedMessage)
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
