using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace PlzOpenMe
{
    public class Program
    {
        /// <summary>
        /// set up the the main server loop
        /// </summary>
        /// <param name="args">program arguments</param>
        public static void Main(string[] args)
        {
            // create the configuration builder
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) +
                                             Path.DirectorySeparatorChar);
            configurationBuilder.AddEnvironmentVariables();
            
            // add the necessary appsettings.json files
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Development)
            {
                // in development mode use the development json and any user secrets
                configurationBuilder.AddJsonFile("appsettings.Development.json", false, true);
                configurationBuilder.AddUserSecrets<Program>();
            } 
            else if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Staging)
            {
                // in staging we will only use staging json
                configurationBuilder.AddJsonFile("appsettings.Staging.json", false, true);
            }
            else
            {
                // in production we will load both production json files
                configurationBuilder.AddJsonFile("appsettings.json", false, true);
                configurationBuilder.AddJsonFile("appsettings.Production.json", true, true);
            }

            // build the configuration
            IConfiguration configuration = configurationBuilder.Build();

            // create the logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            
            // start the host
            try
            {
                Log.Information($"Starting web host");
                CreateHostBuilder(args, configuration).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Actually builds and starts the host
        /// </summary>
        /// <param name="args">program args</param>
        /// <param name="configuration">loaded configuration</param>
        /// <returns>running host</returns>
        public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder => { webBuilder
                    .UseUrls(configuration.GetSection("Urls").GetChildren()?.Select(x => x.Value)?.ToArray())
                    .UseStartup<Startup>(); });
    }
}