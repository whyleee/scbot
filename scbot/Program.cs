using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Perks;
using scbot.Config;
using scbot.Config.Json;
using scbot.Repo;

namespace scbot
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var options = new Options();
            var ui = new ConsoleUi(options);
            var config = new JsonConfig();
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

            var installer = new SitecoreInstaller();
            var repo = new SitecorePackageRepository(ui);
            var ok = false;

            if (command == "install")
            {
                options.Common = options.Install;

                IDictionary<string, string> userParams = null;

                if (!string.IsNullOrEmpty(options.Install.ConfigPath))
                {
                    userParams = config.ReadConfig(options.Install.ConfigPath);
                }

                installer.InitMinSupportedSqlServerVersion(); // required for SQLUtil

                if (userParams == null)
                {
                    Console.Write("No config provided ('-c' param). ");
                    Console.WriteLine(options.Common.SimpleMode
                        ? "Generating config..."
                        : "Answer the questions below...");

                    var configGenerator = new InteractiveConfigGenerator(ui, options);
                    var generatedConfig = configGenerator.Generate();

                    // can't install site with all default settings
                    if (options.Common.SimpleMode)
                    {
                        Environment.Exit(0);
                    }

                    var installSite = ui.AskYesNo("install site", @default: "y");

                    if (!installSite)
                    {
                        Environment.Exit(0);
                    }

                    userParams = config.ParseParams(generatedConfig);
                }

                var sitecoreVersion = options.Install.Version;

                if (userParams.ContainsKey(SitecoreMsiParams.SitecoreVersion))
                {
                    sitecoreVersion = userParams[SitecoreMsiParams.SitecoreVersion];
                }

                if (sitecoreVersion == "latest")
                {
                    sitecoreVersion = null;
                }

                var sdnUsername = options.Install.SdnUsername;

                if (userParams.ContainsKey(SitecoreMsiParams.SdnUsername))
                {
                    sdnUsername = userParams[SitecoreMsiParams.SdnUsername];
                }

                var sdnPassword = options.Install.SdnPassword;

                if (userParams.ContainsKey(SitecoreMsiParams.SdnPassword))
                {
                    sdnPassword = userParams[SitecoreMsiParams.SdnPassword];
                }

                var sitecorePackage = repo.GetPackage(sitecoreVersion, sdnUsername, sdnPassword);
                installer.InitRuntimeParams(sitecorePackage);

                ok = installer.Install(sitecorePackage, options, userParams);
            }

            if (!ok)
            {
                Console.WriteLine("ERROR: not installed");
            }
        }
    }
}
