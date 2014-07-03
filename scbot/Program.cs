using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;

namespace scbot
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
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

            if (command == "install")
            {
                ok = installer.Install(options);
            }

            if (!ok)
            {
                Console.WriteLine("ERROR: not installed");
            }
        }
    }
}
