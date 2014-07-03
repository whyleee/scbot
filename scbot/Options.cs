using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace scbot
{
    public class Options
    {
        [VerbOption("install", HelpText = "Create a new Sitecore instance")]
        public InstallOptions Install { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }

    public class InstallOptions
    {
        [Option('c', "config", Required = true, HelpText = "Path to the config with install parameters")]
        public string ConfigPath { get; set; }

        [Option('m', "mode", DefaultValue = InstallMode.Full, HelpText = "Specify install mode: 'full', 'db' or 'client'")]
        public InstallMode Mode { get; set; }
    }

    [Flags]
    public enum InstallMode
    {
        Db = 1,
        Client = 2,
        Full = 3
    }
}
