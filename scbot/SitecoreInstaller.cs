using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommandLine;
using Newtonsoft.Json;
using scbot.Repo;
using SitecoreInstallWizardCore.RuntimeInfo;
using SitecoreInstallWizardCore.Utils;

namespace scbot
{
    public class SitecoreInstaller
    {
        private const string SITECORE_MSI_PATH = "Sitecore.msi"; // TODO: configurable
        private const string SITECORE_INSTALLER_PATH = "InstallWizard.exe";
        private const string REPO_DIR = @"%AppData%\scbot";

        public void DownloadLatestInstallerFromSdn()
        {
            var sdnClient = new SitecoreSdnClient();
            sdnClient.Login("username", "password");
            var sitecorePackage = sdnClient.GetLatestSitecorePackage();

            Console.WriteLine("Found latest Sitecore " + sitecorePackage.Version + ". Downloading...");

            var outDir = Path.Combine(REPO_DIR, sitecorePackage.Version);

            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            var outFile = Path.Combine(outDir, "sitecore_installer.exe");
            sdnClient.DownloadFile(sitecorePackage.DownloadUrl, outFile);

            Console.WriteLine("Downloaded.");
        }

        public void InitRuntimeParams()
        {
            // sitecore runtime params
            var exeDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var installerAssemblyPath = Path.Combine(exeDir, SITECORE_INSTALLER_PATH);
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

        public bool Install(Options options)
        {
            var installDb = (options.Install.Mode & InstallMode.Db) != 0;
            var installClient = (options.Install.Mode & InstallMode.Client) != 0;

            var uniqueInstanceId = SitecoreInstances.GetAvailableInstanceId(SITECORE_MSI_PATH);
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
                SITECORE_MSI_PATH, "scbot.install.log", MakeMsiParams(installParams));

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
