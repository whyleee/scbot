using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace scbot.Repo
{
    public class SitecorePackageRepository
    {
        private const string REPO_DIR = @"%AppData%\scbot";

        private readonly ConsoleUi _ui;
        private readonly SitecoreSdnClient _sdnClient = new SitecoreSdnClient();

        public SitecorePackageRepository(ConsoleUi ui)
        {
            _ui = ui;
        }

        public SitecorePackage GetPackage(string version, string sdnUsername = null, string sdnPassword = null)
        {
            var loggedIn = _ui.AskCredentials(
                credentialsTest: _sdnClient.Login,
                title: "Sitecore SDN credentials",
                message: "Provide your credentials for http://sdn.sitecore.net/",
                username: sdnUsername,
                password: sdnPassword
            );

            if (!loggedIn)
            {
                Console.WriteLine("ERROR: Sitecore SDN credentials are required.");
                Environment.Exit(-1);
            }

            var package = _sdnClient.GetSitecorePackage(version);

            if (version == null)
            {
                Console.WriteLine("Found latest Sitecore {0}.", package.Version);
            }
            else
            {
                Console.WriteLine("Using Sitecore {0}...", version);
            }

            var packageDir = CreatePackageDir(package);
            var installerFile = GetInstaller(package, packageDir);
            var unpackedInstallerDir = GetUnpackedInstallerDir(packageDir, installerFile);
            var unpackedMsiDir = GetUnpackedMsiDir(unpackedInstallerDir);

            package.LocalPaths = new SitecorePackagePaths
            {
                PackageDir = packageDir,
                MsiPath = Path.Combine(unpackedMsiDir, "Sitecore.msi"),
                WizardPath = Path.Combine(unpackedMsiDir, "InstallWizard.exe")
            };

            return package;
        }

        private string CreatePackageDir(SitecorePackage package)
        {
            var repoDir = Environment.ExpandEnvironmentVariables(REPO_DIR);
            var packageDir = Path.Combine(repoDir, package.Version);

            if (!Directory.Exists(packageDir))
            {
                Directory.CreateDirectory(packageDir);
            }

            return packageDir;
        }

        private string GetInstaller(SitecorePackage package, string packageDir)
        {
            var installerFile = Path.Combine(packageDir, "sitecore_installer.exe");

            if (!File.Exists(installerFile))
            {
                Console.WriteLine("Downloading Sitecore package...");
                _sdnClient.DownloadFile(package.DownloadUrl, installerFile);
            }

            return installerFile;
        }

        private string GetUnpackedInstallerDir(string packageDir, string installerFile)
        {
            var unpackedInstallerDir = Path.Combine(packageDir, "unpacked");

            if (!Directory.Exists(unpackedInstallerDir) || !Directory.GetFileSystemEntries(unpackedInstallerDir).Any())
            {
                Directory.CreateDirectory(unpackedInstallerDir);

                Console.WriteLine("Unpacking installer...");
                ExtractArchive(installerFile, unpackedInstallerDir);
            }

            return unpackedInstallerDir;
        }

        private string GetUnpackedMsiDir(string unpackedInstallerDir)
        {
            var cabDirPath = Path.Combine(unpackedInstallerDir, @".rsrc\RES_CAB");
            var cabPath = Path.Combine(cabDirPath, "SETUP_BOOTSTRAP_1.CAB");
            var unpackedMsiDir = Path.Combine(cabDirPath, "exe");

            if (!Directory.Exists(unpackedMsiDir))
            {
                Console.WriteLine("Unpacking CAB's...");
                ExtractArchive(cabPath, cabDirPath);
            }

            return unpackedMsiDir;
        }

        private void ExtractArchive(string archivePath, string extractPath)
        {
            var sevenZArgs = string.Format("x \"{0}\" -o\"{1}\" -y", archivePath, extractPath);
            var sevenZ = new ProcessStartInfo("7z", sevenZArgs)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(sevenZ))
            {
                var output = process.StandardOutput.ReadToEnd();

                if (process.ExitCode != 0)
                {
                    output = RemoveLines(output, 5);
                    Console.WriteLine("Error executing 7-zip: " + output);
                }
            }
        }

        private string RemoveLines(string s, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var endLineIndex = s.IndexOf(Environment.NewLine, StringComparison.OrdinalIgnoreCase);

                if (endLineIndex == -1 || endLineIndex + 2 > s.Length - 1)
                {
                    break;
                }

                s = s.Substring(endLineIndex + 2);
            }

            return s;
        }
    }
}
