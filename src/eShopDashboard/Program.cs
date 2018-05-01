using eShopDashboard.Infrastructure.Data.Catalog;
using eShopDashboard.Infrastructure.Data.Ordering;
using eShopDashboard.Infrastructure.Setup;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace eShopDashboard
{
    public class Program
    {
        private static BackgroundWorker _bw = new BackgroundWorker
        {
            WorkerReportsProgress = true
        };

        private static int _seedingProgress = 0;

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

        public static int GetSeedingProgress()
        {
            return _seedingProgress;
        }

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
                _bw.ProgressChanged += ReportProgress;
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

        private static void ReportProgress(object sender, ProgressChangedEventArgs eventArgs)
        {
            _seedingProgress = eventArgs.ProgressPercentage;
        }

        private static void SeedDatabase(object sender, DoWorkEventArgs eventArgs)
        {
            try
            {
                var host = (IWebHost)eventArgs.Argument;

                for (int i = 0; i <= 100; i += 10)
                {
                    Log.Debug("----- SeedDatabase: {Percent}", i);

                    _bw.ReportProgress(i);
                    Thread.Sleep(500);
                }

                Log.Information("----- Seeding Database");


                using (var scope = host.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;

                    var catalogContextSetup = services.GetService<CatalogContextSetup>();
                    catalogContextSetup.SeedAsync().Wait();

                    var orderingContextSetup = services.GetService<OrderingContextSetup>();
                    orderingContextSetup.SeedAsync().Wait();
                }

                Log.Information("----- Database Seeded");

            }
            catch (Exception ex)
            {
                Log.Error(ex, "----- Exception seeding database");
            }
        }
    }
}