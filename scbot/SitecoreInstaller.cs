using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Perks;
using scbot.Config;
using scbot.Config.Resources;
using scbot.Repo;
using SitecoreInstallWizardCore.RuntimeInfo;
using SitecoreInstallWizardCore.Utils;

namespace scbot
{
    public class SitecoreInstaller
    {
        private readonly AssemblyXmlResourceConfigReader _runtimeConfig = new AssemblyXmlResourceConfigReader();

        public void InitMinSupportedSqlServerVersion()
        {
            // SQL Server 2008 R2 SP1 - min supported version from Sitecore 7.0 to the latest version
            RuntimeParameters.SetParameter(SitecoreInstallerParams.SqlServerMajorVersion, "10");
            RuntimeParameters.SetParameter(SitecoreInstallerParams.SqlServerMinorVersion, "50");
            RuntimeParameters.SetParameter(SitecoreInstallerParams.SqlServerBuildVersion, "2500");
        }

        public void InitRuntimeParams(SitecorePackage sitecorePackage)
        {
            Ensure.ArgumentNotNull(sitecorePackage, "sitecorePackage");

            var runtimeParams = _runtimeConfig
                .ReadConfig(sitecorePackage.LocalPaths.WizardPath)
                .Select(param => new RuntimeParameter(param.Key, param.Value))
                .ToList();

            RuntimeParameters.SetParameters(runtimeParams);
        }

        public bool Install(SitecorePackage sitecorePackage, Options options, IDictionary<string, string> userParams)
        {
            Ensure.ArgumentNotNull(sitecorePackage, "sitecorePackage");
            Ensure.ArgumentNotNull(options, "options");
            Ensure.ArgumentNotNull(userParams, "userParams");

            var installParams = CreateInstallParams(sitecorePackage, options);

            AddUserParams(userParams, installParams);

            var ok = RunMsi(sitecorePackage, installParams);

            if (ok)
            {
                DoCustomInstallSteps(installParams);
            }

            return ok;
        }

        private IDictionary<string, string> CreateInstallParams(SitecorePackage sitecorePackage, Options options)
        {
            var installDb = (options.Install.Mode & InstallMode.Db) != 0;
            var installClient = (options.Install.Mode & InstallMode.Client) != 0;

            var uniqueInstanceId = SitecoreInstances.GetAvailableInstanceId(sitecorePackage.LocalPaths.MsiPath);
            var installParams = new Dictionary<string, string>
            {
                {SitecoreMsiParams.MsiTransforms, string.Format(":InstanceId{0};:ComponentGUIDTransform{0}.mst", uniqueInstanceId)},
                {SitecoreMsiParams.MsiNewInstance, "1"},
                {SitecoreMsiParams.MsiLogVerbose, "1"},
            };

            if (installClient)
            {
                SetClientParams(installParams);
            }

            return installParams;
        }

        private void SetClientParams(IDictionary<string, string> installParams)
        {
            if (!IsAdministrator())
            {
                Console.WriteLine("ERROR: you need administrator rights to create a website in IIS.");
                Environment.Exit(-1);
            }

            installParams.Add(SitecoreMsiParams.IisSiteID, IisUtil.GetUniqueSiteID().ToString());
        }

        private void AddUserParams(IDictionary<string, string> userParams, IDictionary<string, string> installParams)
        {
            foreach (var @param in userParams)
            {
                installParams[@param.Key] = @param.Value;
            }

            // TODO: if SqlServerConfig{User|Password} set, create SQL user if not exists
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

        private bool RunMsi(SitecorePackage sitecorePackage, IDictionary<string, string> installParams)
        {
            var logFileName = string.Format("scbot.install.{0}.log", DateTime.Now.ToString("yyyy-MM-dd'.'HH-mm-ss"));
            var logPath = Path.Combine(sitecorePackage.LocalPaths.PackageDir, logFileName);

            var msiArgs = string.Format("/i \"{0}\" /l*+v \"{1}\" {2}",
                sitecorePackage.LocalPaths.MsiPath, logPath, MakeMsiParams(installParams));

            Console.WriteLine("Executing msiexec: " + msiArgs);

            using (var msi = Process.Start("msiexec", msiArgs))
            {
                msi.WaitForExit();

                return msi.ExitCode == 0;
            }
        }

        private void DoCustomInstallSteps(IDictionary<string, string> installParams)
        {
            // create SQL config user if required but not exists
            if (installParams.ContainsKey(SitecoreMsiParams.SqlServerConfigUser) &&
                installParams.ContainsKey(SitecoreMsiParams.SqlServerConfigPassword))
            {
                var configUsername = installParams[SitecoreMsiParams.SqlServerConfigUser];
                var configPassword = installParams[SitecoreMsiParams.SqlServerConfigPassword];

                var dbPrefix = installParams[SitecoreMsiParams.SqlDbPrefix];
                var mapToDbs = new[] { dbPrefix + "Sitecore_Core", dbPrefix + "Sitecore_Master", dbPrefix + "Sitecore_Web" };

                var dbHelper = new SitecoreDbHelper(
                    sqlServer: installParams[SitecoreMsiParams.SqlServer],
                    sqlUser: installParams[SitecoreMsiParams.SqlServerUser],
                    sqlPassword: installParams[SitecoreMsiParams.SqlServerPassword]
                );

                dbHelper.CreateSqlConfigUserIfNotExists(configUsername, configPassword, mapToDbs);
            }
        }

        private bool IsAdministrator()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            var isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

            return isElevated;
        }
    }
}
