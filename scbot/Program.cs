﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using SitecoreInstallWizardCore.RuntimeInfo;
using SitecoreInstallWizardCore.Utils;

namespace scbot
{
    class Program
    {
        class Options
        {
            [VerbOption("install", HelpText = "Create a new Sitecore instance")]
            public InstallOptions Install { get; set; }

            [HelpVerbOption]
            public string GetUsage(string verb)
            {
                return HelpText.AutoBuild(this, verb);
            }
        }

        class InstallOptions
        {
            [Option('c', "config", Required = true, HelpText = "Path to the config with install parameters")]
            public string ConfigPath { get; set; }

            [Option('m', "mode", DefaultValue = InstallMode.Full, HelpText = "Specify install mode: 'full', 'db' or 'client'")]
            public InstallMode Mode { get; set; }
        }

        [Flags]
        enum InstallMode
        {
            Db = 1,
            Client = 2,
            Full = 3
        }

        static void Main(string[] args)
        {
            const string SITECORE_MSI_PATH = "Sitecore.msi"; // TODO: configurable

            var options = new Options();
            string command = null;
            object commandOptions = null;

            if (!Parser.Default.ParseArguments(args, options, (verb, subOptions) =>
            {
                command = verb;
                commandOptions = subOptions;
            }))
            {
                Environment.Exit(Parser.DefaultExitCodeFail);
            }

            if (command == "install")
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

                Console.WriteLine("Done!");
            }
        }

        static IDictionary<string, string> ReadParams(string configPath)
        {
            var jsonReader = new JsonTextReader(new StreamReader(configPath));
            var @params = new JsonSerializer().Deserialize<Dictionary<string, string>>(jsonReader);

            return @params;
        }

        static string MakeMsiParams(IDictionary<string, string> @params)
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

        static bool IsAdministrator()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            var isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

            return isElevated;
        }
    }
}