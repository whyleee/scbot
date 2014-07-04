using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using scbot.Config.Json;
using SitecoreInstallWizardCore.Utils;

namespace scbot.Config
{
    public class InteractiveConfigGenerator
    {
        private readonly ConsoleUi _ui;
        private readonly Options _options;

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
            config.SetInstallMode(_options.Install.Mode);

            // general settings
            config.InstanceName = _ui.AskQuestion("instance name", @default: currentDirName);
            var simpleInstanceName = config.InstanceName.Replace(" ", "").ToLower();
            config.Language = _ui.AskQuestion("language", @default: "en-US");
            config.LicensePath = _ui.AskQuestion("license path", @default: simpleMode ? @"C:\Sitecore\license.xml" : null);
            config.InstallFolder = _ui.AskQuestion("install path", @default: currentDir);
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
                config.SqlDbPrefix = _ui.AskQuestion("sql db prefix", @default: config.InstanceName);
                config.SqlPrefixPhysicalFiles = bool.Parse(_ui.AskQuestion("prefix sql files", @default: "y", yesno: true));
            }

            if (!config.SkipConfigureIis)
            {
                config.SqlServerConfigUser = _ui.AskQuestion("sql server config user", @default: config.SqlServerUser);
                config.SqlServerConfigPassword = _ui.AskQuestion("sql server config password", @default: config.SqlServerPassword);
            }

            // iis settings
            if (!config.SkipConfigureIis)
            {
                config.NetVersion = _ui.AskQuestion(".net version", "4");
                config.IisSiteName = _ui.AskQuestion("iis site name", @default: config.InstanceName);
                config.IisAppPoolName = _ui.AskQuestion("iis app pool name", @default: config.InstanceName);
                config.IisIntegratedPipelineMode = bool.Parse(_ui.AskQuestion("iis integrated mode", @default: "y", yesno: true));
                config.IisSiteHostname = _ui.AskQuestion("site hostname", @default: simpleInstanceName);
                config.IisSitePort = _ui.AskQuestion("site port", @default: "80");
            }

            var saveJson = bool.Parse(_ui.AskQuestion("save json file", @default: "y", yesno: true));

            if (saveJson)
            {
                var jsonPath = _ui.AskQuestion("save path", @default: string.Format("scbot.{0}.json", simpleInstanceName));
                SaveToJson(jsonPath, config);

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

        private void SaveToJson(string jsonPath, SitecoreInstallParameters config)
        {
            // serialize to json
            var serializeSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Converters = new JsonConverter[] { new SitecoreBoolConverter() }
            };
            var json = JsonConvert.SerializeObject(config, serializeSettings);

            // add comments
            var jsonLines = json.Split(new[] {Environment.NewLine}, StringSplitOptions.None).ToList();
            jsonLines.Insert(1, "  // General settings");
            jsonLines.Insert(jsonLines.FindIndex(line => line.Contains(SitecoreMsiParams.Installlocation)), "  // Install paths");
            jsonLines.Insert(jsonLines.FindIndex(line => line.Contains(SitecoreMsiParams.DatabaseType)), "  // DB settings");
            jsonLines.Insert(jsonLines.FindIndex(line => line.Contains(SitecoreMsiParams.NetVersion)), "  // IIS settings");

            // write to file
            File.WriteAllLines(jsonPath, jsonLines);
        }
    }
}
