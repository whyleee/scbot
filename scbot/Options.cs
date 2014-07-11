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

        public CommonOptions Common { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }

    public abstract class CommonOptions
    {
        [Option('s', "simple", HelpText = "Simple mode: only basic settings required")]
        public bool SimpleMode { get; set; }
    }

    public class InstallOptions : CommonOptions
    {
        [Option('c', "config", HelpText = "Path to the config with install parameters")]
        public string ConfigPath { get; set; }

        [Option('m', "mode", DefaultValue = InstallMode.Full, HelpText = "Specify install mode: 'full', 'db' or 'client'")]
        public InstallMode Mode { get; set; }

        [Option('v', "version", HelpText = "Sitecore version to install (format: 7.2 rev. 140526). Default: latest version")]
        public string Version { get; set; }
    }

    [Flags]
    public enum InstallMode
    {
        Db = 1,
        Client = 2,
        Full = 3
    }
}
