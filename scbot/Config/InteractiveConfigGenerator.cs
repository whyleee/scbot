using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Perks;
using scbot.Config.Json;
using scbot.Repo;
using SitecoreInstallWizardCore.Utils;

namespace scbot.Config
{
    public class InteractiveConfigGenerator
    {
        private readonly ConsoleUi _ui;
        private readonly Options _options;
        private readonly IConfigWriter _configWriter = new JsonConfig();

        public InteractiveConfigGenerator(ConsoleUi ui, Options options)
        {
            _ui = ui;
            _options = options;
        }

        public SitecoreInstallParameters Generate()
        {
            var simpleMode = _options.Common.SimpleMode;
            var currentDir = Directory.GetCurrentDirectory();
            var currentDirName = Path.GetFileName(currentDir);

            var config = new SitecoreInstallParameters();

            // general settings
            config.InstanceName = _ui.AskQuestion("instance name",
                @default: _options.Install.InstanceName.IfNotNullOrEmpty() ?? currentDirName
            );
            var simpleInstanceName = config.InstanceName.Replace(" ", "").ToLower();

            config.SitecoreVersion = _ui.AskQuestion("sitecore version",
                @default: _options.Install.Version.IfNotNullOrEmpty() ?? "latest",
                validator: answer => Regex.IsMatch(answer, @"(\d.\d ?(rev)?\. ?\d{6})|latest")
            );

            var installModeAnswer = _ui.AskQuestion("install mode (full|db|client)",
                @default: _options.Install.Mode.ToString().ToLower(),
                validator: answer => Regex.IsMatch(answer, @"(full|db|client)", RegexOptions.IgnoreCase)
            );
            var parsedInstallMode = (InstallMode) Enum.Parse(typeof (InstallMode), installModeAnswer, ignoreCase: true);
            config.SetInstallMode(parsedInstallMode);

            config.Language = _ui.AskQuestion("language",
                @default: "en-US",
                validator: answer => CultureInfo.GetCultures(CultureTypes.AllCultures).Any(ci => ci.Name == answer)
            );
            config.LicensePath = _ui.AskFile("license path",
                dialogTitle: "Select Sitecore license file",
                fileFilter: "Sitecore license files (*.xml)|*.xml",
                @default: simpleMode ? @"C:\Sitecore\license.xml" : null
            );
            config.InstallFolder = _ui.AskQuestion("install path",
                @default: _options.Install.InstallPath.IfNotNullOrEmpty() ?? currentDir
            );
            config.DataFolder = _ui.AskQuestion("data path", @default: Path.Combine(config.InstallFolder, "App_Data"));

            // sql settings
            if (!config.SkipInstallSqlData)
            {
                config.DbFolder = _ui.AskQuestion("db path", @default: Path.Combine(config.DataFolder, "db"));
                config.DbMdfFolder = _ui.AskQuestion("db mdf path", @default: config.DbFolder);
                config.DbLdfFolder = _ui.AskQuestion("db ldf path", @default: config.DbFolder);
            }

            config.DbType = "MSSQL"; // no oracle support
            config.SqlServer = AskForSqlServer(config, _ui);

            if (!config.SkipInstallSqlData)
            {
                config.SqlServerUser = _ui.AskQuestion("sql server user", @default: "sa");
                config.SqlServerPassword = _ui.AskQuestion("sql server password", @default: simpleMode ? "sa_password" : null);
            }

            config.SqlDbPrefix = _ui.AskQuestion("sql db prefix", @default: config.InstanceName);
            config.SqlPrefixPhysicalFiles = _ui.AskYesNo("prefix sql files", @default: "y");

            if (!config.SkipConfigureIis)
            {
                config.SqlServerConfigUser = _ui.AskQuestion("sql server config user", @default: config.SqlServerUser);
                config.SqlServerConfigPassword = _ui.AskQuestion("sql server config password", @default: config.SqlServerPassword);
            }

            // iis settings
            if (!config.SkipConfigureIis)
            {
                config.NetVersion = _ui.AskQuestion(".net version", @default: "4", validator: answer => Regex.IsMatch(answer, @"\d"));
                config.IisSiteName = _ui.AskQuestion("iis site name", @default: config.InstanceName);
                config.IisAppPoolName = _ui.AskQuestion("iis app pool name", @default: config.InstanceName);
                config.IisIntegratedPipelineMode = _ui.AskYesNo("iis integrated mode", @default: "y");
                config.IisSiteHostname = _ui.AskQuestion("site hostname", @default: simpleInstanceName);
                config.IisSitePort = _ui.AskQuestion("site port", @default: "80");
            }

            // sdn settings
            var addSdnCredentials = _ui.AskYesNo("add sdn credentials", @default: "y");

            if (addSdnCredentials)
            {
                var validCredentials = false;
                var sdnClient = new SitecoreSdnClient();

                config.SdnUsername = _options.Install.SdnUsername;
                config.SdnPassword = _options.Install.SdnPassword;

                while (!validCredentials)
                {
                    config.SdnUsername = _ui.AskQuestion("sdn username", @default: config.SdnUsername);
                    config.SdnPassword = _ui.AskQuestion("sdn password", @default: config.SdnPassword);

                    validCredentials = sdnClient.Login(config.SdnUsername, config.SdnPassword);

                    if (!validCredentials)
                    {
                        Console.WriteLine("ERROR: invalid SDN credentials");
                    }
                }
            }

            var saveJson = _ui.AskYesNo("save json file", @default: "y");

            if (saveJson)
            {
                var jsonPath = _ui.AskQuestion("save path", @default: string.Format("scbot.{0}.json", simpleInstanceName));
                _configWriter.WriteConfig(jsonPath, config);

                Console.WriteLine("Config generated: {0}", jsonPath);
            }

            return config;
        }

        private string AskForSqlServer(SitecoreInstallParameters config, ConsoleUi ui)
        {
            Console.WriteLine("Searching for SQL servers...");

            var sqlServers = SQLUtil.GetServers(local: !config.InstallClientMode)
                .Select(server => server.Replace("(local)", "."))
                .ToList();

            if (!_options.Common.SimpleMode)
            {
                Console.WriteLine("Found {0} server{1}:", sqlServers.Count, sqlServers.Count > 1 ? "s" : "");

                for (int i = 0; i < sqlServers.Count; i++)
                {
                    Console.WriteLine("  {0}) {1}", i + 1, sqlServers[i]);
                }
            }

            return ui.AskQuestion("sql server", @default: sqlServers.FirstOrDefault());
        }
    }
}
