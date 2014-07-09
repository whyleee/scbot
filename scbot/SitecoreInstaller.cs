using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Principal;
using System.Text;
using System.Xml.Linq;
using CommandLine;
using CredentialManagement;
using Newtonsoft.Json;
using scbot.Repo;
using SitecoreInstallWizardCore.RuntimeInfo;
using SitecoreInstallWizardCore.Utils;

namespace scbot
{
    public class SitecoreInstaller
    {
        private const string REPO_DIR = @"%AppData%\scbot";

        public SitecorePackage DownloadLatestInstallerFromSdn()
        {
            var sdnClient = new SitecoreSdnClient();
            var sdnCredentials = new Credential {Target = "scbot"};
            var loggedIn = false;

            if (sdnCredentials.Exists())
            {
                sdnCredentials.Load();
                loggedIn = sdnClient.Login(sdnCredentials.Username, sdnCredentials.Password);
            }

            if (!loggedIn)
            {
                using (var prompt = new VistaPrompt())
                {
                    prompt.Title = "Sitecore SDN credentials";
                    prompt.Message = "Provide your credentials for http://sdn.sitecore.net/";
                    prompt.GenericCredentials = true;
                    prompt.ShowSaveCheckBox = true;

                    while (!loggedIn)
                    {
                        var result = prompt.ShowDialog();

                        if (result == DialogResult.OK)
                        {
                            loggedIn = sdnClient.Login(prompt.Username, prompt.Password);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (prompt.SaveChecked)
                    {
                        sdnCredentials = new Credential(prompt.Username, prompt.Password, "scbot", CredentialType.Generic);
                        sdnCredentials.Save();
                    }
                }
            }

            if (!loggedIn)
            {
                Console.WriteLine("ERROR: Sitecore SDN credentials are required.");
                Environment.Exit(-1);
            }
            
            var sitecorePackage = sdnClient.GetLatestSitecorePackage();

            Console.WriteLine("Found latest Sitecore " + sitecorePackage.Version + ". Downloading...");

            // create repo dir if not exists
            var repoDir = Environment.ExpandEnvironmentVariables(REPO_DIR);
            var outDir = Path.Combine(repoDir, sitecorePackage.Version);

            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            // download installer if not exists
            var outFile = Path.Combine(outDir, "sitecore_installer.exe");

            if (!File.Exists(outFile))
            {
                sdnClient.DownloadFile(sitecorePackage.DownloadUrl, outFile);
                Console.WriteLine("Downloaded.");
            }

            // unpack installer if not unpacked yet
            var unpackedInstallerDir = Path.Combine(outDir, "unpacked");

            if (!Directory.Exists(unpackedInstallerDir) || !Directory.GetFileSystemEntries(unpackedInstallerDir).Any())
            {
                Directory.CreateDirectory(unpackedInstallerDir);

                Console.WriteLine("Unpacking installer...");

                var sevenZArgs = string.Format("x \"{0}\" -o\"{1}\" -y", outFile, unpackedInstallerDir);
                var startInfo = new ProcessStartInfo("7z", sevenZArgs);
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                using (var process = Process.Start(startInfo))
                {
                    //var output = process.StandardOutput.ReadToEnd();

                    //Console.WriteLine("Installer is unpacked. Output: " + output);
                }

                Console.WriteLine("Installer unpacked.");
            }

            // unpack CAB's if not unpacked yet
            var cabDirPath = Path.Combine(unpackedInstallerDir, @".rsrc\RES_CAB");
            var cabPath = Path.Combine(cabDirPath, "SETUP_BOOTSTRAP_1.CAB");
            var unpackedMsiDir = Path.Combine(cabDirPath, "exe");

            if (!Directory.Exists(unpackedMsiDir))
            {
                Console.WriteLine("Unpacking CAB's...");

                var sevenZArgs = string.Format("x \"{0}\" -o\"{1}\" -y", cabPath, cabDirPath);
                var startInfo = new ProcessStartInfo("7z", sevenZArgs);
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;

                // unpack cab with msi
                using (var process = Process.Start(startInfo))
                {
                    var output = process.StandardOutput.ReadToEnd();

                    if (process.ExitCode != 0)
                    {
                        output = RemoveLines(output, 5);
                        Console.WriteLine("Error executing 7-zip: " + output);
                    }
                }

                Console.WriteLine("CAB's unpacked.");
            }

            sitecorePackage.LocalPaths = new SitecorePackagePaths
            {
                MsiPath = Path.Combine(unpackedMsiDir, "Sitecore.msi"),
                WizardPath = Path.Combine(unpackedMsiDir, "InstallWizard.exe")
            };

            return sitecorePackage;
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

        public void InitRuntimeParams(SitecorePackage sitecorePackage)
        {
            // sitecore runtime params
            var exeDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var installerAssemblyPath = Path.Combine(exeDir, sitecorePackage.LocalPaths.WizardPath);
            var installerAssembly = Assembly.LoadFile(installerAssemblyPath);
            var installerResources = new ResourceManager("InstallWizard.g", installerAssembly);

            using (var installerXmlConfigStream = installerResources.GetStream("configuration/configuration.xml"))
            {
                var runtimeParams = XDocument.Load(installerXmlConfigStream)
                    .Root
                    .Element("Parameters")
                    .Elements("Param")
                    .Select(el => new RuntimeParameter(
                        key: el.Attribute("Name").Value,
                        value: el.Attribute("Value").Value)
                    ).ToList();

                RuntimeParameters.SetParameters(runtimeParams);
            }
        }

        public bool Install(SitecorePackage sitecorePackage, Options options)
        {
            var installDb = (options.Install.Mode & InstallMode.Db) != 0;
            var installClient = (options.Install.Mode & InstallMode.Client) != 0;

            var uniqueInstanceId = SitecoreInstances.GetAvailableInstanceId(sitecorePackage.LocalPaths.MsiPath);
            var installParams = new Dictionary<string, string>
                {
                    {"TRANSFORMS", string.Format(":InstanceId{0};:ComponentGUIDTransform{0}.mst", uniqueInstanceId)},
                    {"MSINEWINSTANCE", "1"},
                    {"LOGVERBOSE", "1"},
                };

            // specify install mode for msi
            if (options.Install.Mode == InstallMode.Full)
            {
                installParams.Add("SC_FULL", "1");
            }
            else if (options.Install.Mode == InstallMode.Db)
            {
                installParams.Add("SC_DBONLY", "1");
            }
            else if (options.Install.Mode == InstallMode.Client)
            {
                installParams.Add("SC_CLIENTONLY", "1");
            }

            // set unique IIS site ID
            if (installClient)
            {
                if (!IsAdministrator())
                {
                    Console.WriteLine("ERROR: you need administrator rights to create a website in IIS.");
                    Environment.Exit(Parser.DefaultExitCodeFail);
                }

                installParams.Add("SC_IISSITE_ID", IisUtil.GetUniqueSiteID().ToString());
            }

            // read user params
            var userParams = ReadParams(options.Install.ConfigPath);

            foreach (var @param in userParams)
            {
                installParams.Add(@param.Key, @param.Value);
            }

            installParams.Add("SC_SQL_SERVER_CONFIG_USER", installParams["SC_SQL_SERVER_USER"]);
            installParams.Add("SC_SQL_SERVER_CONFIG_PASSWORD", installParams["SC_SQL_SERVER_PASSWORD"]);

            // run msi install
            var msiArgs = string.Format("/i \"{0}\" /l*+v \"{1}\" {2}",
                sitecorePackage.LocalPaths.MsiPath, "scbot.install.log", MakeMsiParams(installParams));

            Console.WriteLine("Executing msiexec: " + msiArgs);

            var msi = Process.Start("msiexec", msiArgs);
            msi.WaitForExit();

            return msi.ExitCode == 0;
        }

        private IDictionary<string, string> ReadParams(string configPath)
        {
            var jsonReader = new JsonTextReader(new StreamReader(configPath));
            var @params = new JsonSerializer().Deserialize<Dictionary<string, string>>(jsonReader);

            return @params;
        }

        private string MakeMsiParams(IDictionary<string, string> @params)
        {
            var msiParams = new StringBuilder();

            foreach (var @param in @params)
            {
                msiParams.Append(string.Format("{0}=\"{1}\" ", @param.Key, @param.Value));
            }

            // remove last space
            if (msiParams.Length > 0)
            {
                msiParams.Remove(msiParams.Length - 1, 1);
            }

            return msiParams.ToString();
        }

        private bool IsAdministrator()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            var isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

            return isElevated;
        }
    }
}
