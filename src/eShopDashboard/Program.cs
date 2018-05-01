using System;
using System.ComponentModel;
using eShopDashboard.Infrastructure.Data.Catalog;
using eShopDashboard.Infrastructure.Setup;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using eShopDashboard.Infrastructure.Data.Ordering;
using Serilog;
using Serilog.Events;

namespace eShopDashboard
{
    public class Program
    {
        static BackgroundWorker _bw = new BackgroundWorker();

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseSerilog()
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    config.AddEnvironmentVariables();
                })
                .Build();

        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341/")
                .CreateLogger();

            Log.Information("Starting web host");

            try
            {
                var host = BuildWebHost(args);

                ConfigureDatabase(host);

                _bw.DoWork += SeedDatabase;
                _bw.RunWorkerAsync(host);

                host.Run();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");

                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureDatabase(IWebHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var catalogContext = services.GetService<CatalogContext>();
                catalogContext.Database.Migrate();

                var orderingContext = services.GetService<OrderingContext>();
                orderingContext.Database.Migrate();
            }
        }

        private static void SeedDatabase(object sender, DoWorkEventArgs eventArgs)
        {
            var host = (IWebHost)eventArgs.Argument;

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var catalogContextSetup = services.GetService<CatalogContextSetup>();
                catalogContextSetup.SeedAsync().Wait();

                var orderingContextSetup = services.GetService<OrderingContextSetup>();
                orderingContextSetup.SeedAsync().Wait();
            }
        }

    }
}