using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using scbot.Config;

namespace scbot
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            var ui = new ConsoleUi(options);
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
            var ok = false;

            installer.InitRuntimeParams();

            if (command == "install")
            {
                options.Common = options.Install;
                SitecoreInstallParameters config = null;

                if (string.IsNullOrEmpty(options.Install.ConfigPath))
                {
                    Console.Write("No config provided ('-c' param). ");
                    Console.WriteLine(options.Common.SimpleMode
                        ? "Generating config..."
                        : "Answer the questions below...");

                    var configGenerator = new InteractiveConfigGenerator(ui, options);
                    config = configGenerator.Generate();

                    Environment.Exit(0);

                    // TODO: impl
                    var installSite = bool.Parse(ui.AskQuestion("install site", @default: "y", yesno: true));
                }

                ok = installer.Install(options);
            }

            if (!ok)
            {
                Console.WriteLine("ERROR: not installed");
            }
        }
    }
}
