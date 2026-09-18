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
            Console.WriteLine($"application : {options.Application}");
            Console.WriteLine($"path        : {options.ApplicationPath}");
            Console.WriteLine($"port        : {options.Port}");
            Console.WriteLine($"module      : {options.GlobalModule ?? "(none)"}");
            Console.WriteLine($"options     : {options.CustomConfigOptions ?? "(none)"}");

            string configPath = options.ConfigPath ?? ApplicationHostConfig.DefaultWorkingCopyPath(id);

            if (!ApplicationHostConfig.CreateWorkingCopy(ApplicationHostConfig.BundledPath, configPath, out string configError))
            {
                Console.Error.WriteLine($"error: {configError}");
                return (int)ExitCode.ConfigurationError;
            }

            Console.WriteLine($"Working configuration: {configPath}");

            IIS.ApplicationPool pool = ApplicationPoolFactory.FromCommandLine(options, id);
            IIS.Site site = SiteFactory.FromCommandLine(options, id);

            // Null unless --globalmodule asked for one.
            GlobalModule module = GlobalModuleFactory.FromCommandLine(options);

            // The section goes into that module, and the parser only accepts
            // --customconfig next to --globalmodule.
            CustomConfig customConfig = module == null ? null : CustomConfigFactory.FromCommandLine(options, id);

            if (!Directory.Exists(options.ApplicationPath))
            {
                Console.Error.WriteLine($"warning: the physical path '{options.ApplicationPath}' does not exist.");
            }

            GlobalModuleBuilder moduleBuilder = null;

            try
            {
                using (ServerManager manager = new ServerManager(configPath))
                {
                    var poolBuilder = new ApplicationPoolBuilder(pool, manager, configPath);

                    if (!poolBuilder.Build(out string poolError))
                    {
                        Console.Error.WriteLine($"error: {poolError}");
                        return (int)ExitCode.ConfigurationError;
                    }

                    var siteBuilder = new SiteBuilder(site, manager, pool, configPath);

                    if (!siteBuilder.Build(out string siteError))
                    {
                        Console.Error.WriteLine($"error: {siteError}");
                        return (int)ExitCode.ConfigurationError;
                    }

                    if (module != null)
                    {
                        moduleBuilder = new GlobalModuleBuilder(module, manager, customConfig, configPath);

                        if (!moduleBuilder.Build(out string moduleError))
                        {
                            Console.Error.WriteLine($"error: {moduleError}");
                            return (int)ExitCode.ConfigurationError;
                        }
                    }

                    // Single commit point for every edit made to the copied configuration.
                    manager.CommitChanges();
                }

                // The custom section follows the commit: its schema is not one
                // the IIS configuration system knows, so it is written as XML
                // and nothing may read the file through that system afterwards.
                if (moduleBuilder != null && !moduleBuilder.BuildCustomConfig(out string customError))
                {
                    Console.Error.WriteLine($"error: {customError}");
                    return (int)ExitCode.ConfigurationError;
                }
            }
            catch (Exception ex) when (ex is COMException || ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"error: could not save '{configPath}': {ex.Message}");
                return (int)ExitCode.ConfigurationError;
            }

            Console.WriteLine($"Added application pool: {pool.Name}");
            Console.WriteLine($"Added site: {site.Name} -> http://localhost:{options.Port}/");

            if (module != null)
            {
                Console.WriteLine($"Added global module: {module.Name} ({module.Image})");
            }

            if (customConfig != null)
            {
                Console.WriteLine($"Added custom section: {customConfig.AppPool} -> {customConfig.Options}");
            }

            return (int)ExitCode.Success;
        }
    }
}
