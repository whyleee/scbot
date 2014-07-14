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
    public class JsonConfigReader : IConfigReader
    {
        public IEnumerable<KeyValuePair<string, string>> ReadConfig(string configPath)
        {
            Ensure.ArgumentNotNullOrEmpty(configPath, "configPath");

            var jsonReader = new JsonTextReader(new StreamReader(configPath));
            var @params = new JsonSerializer().Deserialize<Dictionary<string, string>>(jsonReader);

            return @params;
        }
    }
}
