using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

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

            try
            {
                BuildPackage(package);
            }
            catch (Exception ex)
            {
                var error = ex.Message;

                if (ex.InnerException != null)
                {
                    error += " ---> " + ex.InnerException.Message;
                }

                Console.WriteLine("ERROR: " + error);
                Console.WriteLine("Trying to fix the package...");

                // try to build the package one more time
                BuildPackage(package);
            }

            return package;
        }

        private void BuildPackage(SitecorePackage package)
        {
            SetPackageLocalPaths(package);
            CreatePackageDir(package);
            DownloadPackage(package);
            ExtractPackage(package);
        }

        private void SetPackageLocalPaths(SitecorePackage package)
        {
            var packageDir = GetPackageDir(package);
            package.LocalPaths.PackageDir = packageDir;
            package.LocalPaths.InstallerPath = Path.Combine(package.LocalPaths.PackageDir, "sitecore_installer.exe");

            var extractedPackageDir = Path.Combine(package.LocalPaths.PackageDir, @"SupportFiles\exe");
            package.LocalPaths.MsiPath = Path.Combine(extractedPackageDir, "Sitecore.msi");
            package.LocalPaths.WizardPath = Path.Combine(extractedPackageDir, "InstallWizard.exe");
        }

        private string GetPackageDir(SitecorePackage package)
        {
            var repoDir = Environment.ExpandEnvironmentVariables(REPO_DIR);

            return Path.Combine(repoDir, package.Version);
        }

        private void CreatePackageDir(SitecorePackage package)
        {
            if (!Directory.Exists(package.LocalPaths.PackageDir))
            {
                Directory.CreateDirectory(package.LocalPaths.PackageDir);
            }
        }

        private void DownloadPackage(SitecorePackage package)
        {
            var installerFileExists = File.Exists(package.LocalPaths.InstallerPath);

            if (!installerFileExists || package.Corrupted)
            {
                Console.WriteLine("Downloading Sitecore package...");

                try
                {
                    _sdnClient.DownloadFile(package.DownloadUrl, package.LocalPaths.InstallerPath);
                }
                catch (Exception ex)
                {
                    package.Corrupted = true;
                    throw;
                }
            }
        }

        private void ExtractPackage(SitecorePackage package)
        {
            var msiFileExists = File.Exists(package.LocalPaths.MsiPath);
            var wizardFileExists = File.Exists(package.LocalPaths.WizardPath);

            if (!msiFileExists || !wizardFileExists)
            {
                var packageExe = new ProcessStartInfo(package.LocalPaths.InstallerPath, "/ExtractCab")
                {
                    WorkingDirectory = package.LocalPaths.PackageDir,
                    UseShellExecute = false
                };

                try
                {
                    using (var process = Process.Start(packageExe))
                    {
                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            Console.WriteLine("ERROR: Extraction of Sitecore package failed.");
                            Environment.Exit(-1);
                        }
                    }
                }
                catch (Win32Exception ex)
                {
                    // if Win32 error - then the package is probably corrupted
                    package.Corrupted = true;
                    throw;
                }
            }
        }
    }
}
