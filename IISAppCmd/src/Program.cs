using System;
using System.IO;
using System.Runtime.InteropServices;
using IISAppCmd.CommandLine;
using IISAppCmd.Config;
using IISAppCmd.IIS;
using Microsoft.Web.Administration;

namespace IISAppCmd
{
    internal class Program
    {
        static int Main(string[] args)
        {
            ParseResult result = CommandLineParser.Parse(args);

            if (result.HelpRequested)
            {
                Console.WriteLine(CommandLineParser.HelpText);
                return (int)ExitCode.Success;
            }

            if (result.Error != null)
            {
                Console.Error.WriteLine($"error: {result.Error}");
                Console.Error.WriteLine();
                Console.Error.WriteLine(CommandLineParser.HelpText);
                return (int)ExitCode.UsageError;
            }

            CommandLineOptions options = result.Options;

            // Identifies this run; every artefact it produces is tagged with it.
            string id = Guid.NewGuid().ToString("N").Substring(0, 8);

            Console.WriteLine($"bitness     : {(int)options.Bitness}");
            Console.WriteLine($"tfm         : {options.Tfm}");

            string configPath = options.ConfigPath ?? ApplicationHostConfig.DefaultWorkingCopyPath(id);

            if (!ApplicationHostConfig.CreateWorkingCopy(ApplicationHostConfig.BundledPath, configPath, out string configError))
            {
                Console.Error.WriteLine($"error: {configError}");
                return (int)ExitCode.ConfigurationError;
            }

            Console.WriteLine($"Working configuration: {configPath}");

            IIS.ApplicationPool pool = ApplicationPoolFactory.FromCommandLine(options, id);

            try
            {
                using (ServerManager manager = new ServerManager(configPath))
                {
                    var builder = new ApplicationPoolBuilder(pool, manager, configPath);

                    if (!builder.Build(out string poolError))
                    {
                        Console.Error.WriteLine($"error: {poolError}");
                        return (int)ExitCode.ConfigurationError;
                    }

                    // Single commit point for every edit made to the copied configuration.
                    manager.CommitChanges();
                }
            }
            catch (Exception ex) when (ex is COMException || ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"error: could not save '{configPath}': {ex.Message}");
                return (int)ExitCode.ConfigurationError;
            }

            Console.WriteLine($"Added application pool: {pool.Name}");
            return (int)ExitCode.Success;
        }
    }
}
