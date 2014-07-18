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
            var packageFile = GetInstaller(package, packageDir);
            package.LocalPaths = ExtractPackage(packageFile);

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

        private SitecorePackagePaths ExtractPackage(string packagePath)
        {
            var packageDir = Path.GetDirectoryName(packagePath);
            var extractedPackageDir = Path.Combine(packageDir, @"SupportFiles\exe");
            var packagePaths = new SitecorePackagePaths
            {
                PackageDir = packageDir,
                MsiPath = Path.Combine(extractedPackageDir, "Sitecore.msi"),
                WizardPath = Path.Combine(extractedPackageDir, "InstallWizard.exe")
            };

            if (File.Exists(packagePaths.MsiPath) && File.Exists(packagePaths.WizardPath))
            {
                return packagePaths;
            }

            var packageExe = new ProcessStartInfo(packagePath, "/ExtractCab")
            {
                WorkingDirectory = packageDir,
                UseShellExecute = false
            };

            using (var process = Process.Start(packageExe))
            {
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine("ERROR: Extraction of Sitcore package failed.");
                    Environment.Exit(-1);
                }
            }

            return packagePaths;
        }
    }
}
