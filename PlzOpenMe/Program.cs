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
        public static string AppSettings
        {
            get
            {
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar +
                           "appsettings.Development.json";
                }
                else
                {
                    return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar +
                           "appsettings.json";
                }
            }
        }
        
        // grab the configuration file first thing, need that for building the logger
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(AppSettings, optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // main program
        public static void Main(string[] args)
        {
            Console.WriteLine(AppSettings);
            
            // create the logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
            
            // start the host
            try
            {
                Log.Information($"Starting web host");
                CreateHostBuilder(args).Build().Run();
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

        // start the web host
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder => { webBuilder
                    .UseUrls(Configuration.GetSection("Urls").GetChildren()?.Select(x => x.Value)?.ToArray())
                    .UseStartup<Startup>(); });
    }
}