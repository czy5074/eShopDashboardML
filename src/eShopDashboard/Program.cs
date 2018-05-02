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
using System.Threading.Tasks;

namespace eShopDashboard
{
    public class Program
    {
        private static BackgroundWorker _bw = new BackgroundWorker
        {
            WorkerReportsProgress = true
        };

        private static int _seedingProgress = 100;

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

        public static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341/")
                .CreateLogger();

            Log.Information("----- Starting web host");

            try
            {
                var host = BuildWebHost(args);

                await ConfigureDatabaseAsync(host);

                Log.Information("----- Seeding Database");

                Task seeding = Task.Run(async () => { await SeedDatabaseAsync(host); });

                Log.Information("----- Running Host");

                host.Run();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "----- Host terminated unexpectedly");

                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static async Task ConfigureDatabaseAsync(IWebHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var catalogContext = services.GetService<CatalogContext>();
                await catalogContext.Database.MigrateAsync();

                var orderingContext = services.GetService<OrderingContext>();
                await orderingContext.Database.MigrateAsync();
            }
        }

        private static async Task SeedDatabaseAsync(IWebHost host)
        {

            try
            {
                using (var scope = host.Services.CreateScope())
                {
                    IServiceProvider services = scope.ServiceProvider;

                    Log.Information("----- Checking seeding status");

                    var catalogContextSetup = services.GetService<CatalogContextSetup>();
                    var orderingContextSetup = services.GetService<OrderingContextSetup>();

                    var catalogSeedingStatus = await catalogContextSetup.GetSeedingStatusAsync();
                    var orderingSeedingStatus = await orderingContextSetup.GetSeedingStatusAsync();

                    var seedingStatus = new SeedingStatus(catalogSeedingStatus, orderingSeedingStatus);

                    if (!seedingStatus.NeedsSeeding)
                    {
                        Log.Information("----- No seeding needed");

                        return;
                    }

                    void ProgressAggregator()
                    {
                        seedingStatus.RecordsLoaded = catalogSeedingStatus.RecordsLoaded + orderingSeedingStatus.RecordsLoaded;

                        Log.Debug("----- Seeding {SeedingPercentComplete}% complete", seedingStatus.PercentComplete);
                        _seedingProgress = seedingStatus.PercentComplete;
                    }

                    var catalogProgressHandler = new Progress<int>(value =>
                    {
                        catalogSeedingStatus.RecordsLoaded = value;
                        ProgressAggregator();
                    });

                    var orderingProgressHandler = new Progress<int>(value =>
                    {
                        orderingSeedingStatus.RecordsLoaded = value;
                        ProgressAggregator();
                    });

                    Log.Information("----- Seeding CatalogContext");
                    await catalogContextSetup.SeedAsync(catalogProgressHandler);

                    Log.Information("----- Seeding OrderingContext");
                    await orderingContextSetup.SeedAsync(orderingProgressHandler);

                    seedingStatus.SetAsComplete();
                    _seedingProgress = seedingStatus.PercentComplete;

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