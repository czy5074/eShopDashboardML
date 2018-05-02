using System;
using eShopDashboard.Infrastructure.Data.Ordering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace eShopDashboard.Infrastructure.Setup
{
    public class OrderingContextSetup
    {
        private readonly OrderingContext _dbContext;
        private readonly ILogger<OrderingContextSetup> _logger;
        private readonly string _setupPath;

        private string[] _orderLines;
        private string[] _orderItemLines;
        private SeedingStatus _status;

        public OrderingContextSetup(
            OrderingContext dbContext,
            IHostingEnvironment env,
            ILogger<OrderingContextSetup> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            _setupPath = Path.Combine(env.ContentRootPath, "Infrastructure", "Setup");
        }

        public async Task<SeedingStatus> GetSeedingStatusAsync()
        {
            if (_status != null) return _status;

            if (await _dbContext.Orders.AnyAsync()) return _status = new SeedingStatus(false);

            int dataLinesCount = await GetDataToLoad();

            return _status = new SeedingStatus(dataLinesCount);
        }

        public async Task SeedAsync(IProgress<int> orderingProgressHandler)
        {
            var seedingStatus = await GetSeedingStatusAsync();

            if (!seedingStatus.NeedsSeeding) return;

            _logger.LogInformation($@"----- Seeding OrderingContext from ""{_setupPath}""");

            var ordersLoaded = 0;
            var orderItemsLoaded = 0;

            var ordersProgressHandler = new Progress<int>(value =>
            {
                ordersLoaded = value;
                orderingProgressHandler.Report(ordersLoaded + orderItemsLoaded);
            });

            var orderItemsProgressHandler = new Progress<int>(value =>
            {
                orderItemsLoaded = value;
                orderingProgressHandler.Report(ordersLoaded + orderItemsLoaded);
            });

            await SeedOrdersAsync(ordersProgressHandler);
            await SeedOrderItemsAsync(orderItemsProgressHandler);
        }

        private async Task<int> GetDataToLoad()
        {
            _orderLines = await File.ReadAllLinesAsync(Path.Combine(_setupPath, "Orders.sql"));
            _orderItemLines = await File.ReadAllLinesAsync(Path.Combine(_setupPath, "OrderItems.sql"));

            return _orderLines.Length + _orderItemLines.Length;
        }

        private async Task SeedOrderItemsAsync(IProgress<int> recordsProgressHandler)
        {
            var sw = new Stopwatch();
            sw.Start();

            _logger.LogInformation("----- Seeding OrderItems");

            var batcher = new SqlBatcher(_dbContext.Database, _logger);

            await batcher.ExecuteInsertCommandsAsync(_orderItemLines, recordsProgressHandler);

            _logger.LogInformation($"----- OrderItems Inserted ({sw.Elapsed.TotalSeconds:n3}s)");
        }

        private async Task SeedOrdersAsync(IProgress<int> recordsProgressHandler)
        {
            var sw = new Stopwatch();
            sw.Start();

            _logger.LogInformation("----- Seeding Orders");

            var batcher = new SqlBatcher(_dbContext.Database, _logger);

            await batcher.ExecuteInsertCommandsAsync(_orderLines, recordsProgressHandler);

            _logger.LogInformation($"----- Orders Inserted ({sw.Elapsed.TotalSeconds:n3}s)");
        }
    }
}