using System.Collections.Generic;

namespace scbot.Config
{
    public interface IConfigReader
    {
        IEnumerable<KeyValuePair<string, string>> ReadConfig(string configPath);
    }
}