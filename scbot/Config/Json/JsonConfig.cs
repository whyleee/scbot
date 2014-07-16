using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Perks;

namespace scbot.Config.Json
{
    public class JsonConfig : IConfigReader, IConfigWriter
    {
        private readonly JsonSerializerSettings _serializeSettings;

        public JsonConfig()
        {
            _serializeSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Converters = new JsonConverter[] {new SitecoreBoolConverter()}
            };
        }

        public IDictionary<string, string> ReadConfig(string configPath)
        {
            Ensure.ArgumentNotNullOrEmpty(configPath, "configPath");

            var jsonReader = new JsonTextReader(new StreamReader(configPath));
            var @params = new JsonSerializer().Deserialize<Dictionary<string, string>>(jsonReader);

            return @params;
        }

        public void WriteConfig(string configPath, SitecoreInstallParameters config)
        {
            // serialize to json
            var json = JsonConvert.SerializeObject(config, _serializeSettings);

            // add comments
            var jsonLines = json.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            jsonLines.Insert(1, "  // General settings");
            jsonLines.Insert(jsonLines.FindIndex(line => line.Contains(SitecoreMsiParams.Installlocation)), "  // Install paths");
            jsonLines.Insert(jsonLines.FindIndex(line => line.Contains(SitecoreMsiParams.DatabaseType)), "  // DB settings");

            var iisSectionIndex = jsonLines.FindIndex(line => line.Contains(SitecoreMsiParams.NetVersion));

            if (iisSectionIndex != -1)
            {
                jsonLines.Insert(iisSectionIndex, "  // IIS settings");
            }

            var sdnSettingsIndex = jsonLines.FindIndex(line => line.Contains(SitecoreMsiParams.SdnUsername));

            if (sdnSettingsIndex != -1)
            {
                jsonLines.Insert(sdnSettingsIndex, "  // SDN settings");
            }

            // write to file
            File.WriteAllLines(configPath, jsonLines);
        }

        public IDictionary<string, string> ParseParams(SitecoreInstallParameters config)
        {
            var json = JsonConvert.SerializeObject(config, _serializeSettings);
            var @params = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            return @params;
        }
    }
}
