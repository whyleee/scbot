using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Xml.Linq;
using Perks;

namespace scbot.Config.Resources
{
    public class AssemblyXmlResourceConfigReader : IConfigReader
    {
        public IEnumerable<KeyValuePair<string, string>> ReadConfig(string configPath)
        {
            Ensure.ArgumentNotNullOrEmpty(configPath, "configPath");

            var configPathParts = configPath.Split('@');
            var assemblyPath = configPathParts.First();
            var resourcePath = configPathParts.Length > 1 ? configPathParts.Last() : "configuration/configuration.xml";

            var assembly = Assembly.LoadFile(assemblyPath);
            var installerResources = new ResourceManager(assembly.GetName().Name + ".g", assembly);

            using (var installerXmlConfigStream = installerResources.GetStream(resourcePath))
            {
                var runtimeParams = XDocument.Load(installerXmlConfigStream)
                    .Root
                    .Element("Parameters")
                    .Elements("Param")
                    .Select(el => new KeyValuePair<string, string>(
                        key: el.Attribute("Name").Value,
                        value: el.Attribute("Value").Value)
                    ).ToList();

                return runtimeParams;
            }
        }
    }
}
