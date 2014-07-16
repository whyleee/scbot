using System.Collections.Generic;

namespace scbot.Config
{
    public interface IConfigReader
    {
        IDictionary<string, string> ReadConfig(string configPath);
    }
}