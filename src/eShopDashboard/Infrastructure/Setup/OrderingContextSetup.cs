using System;
using System.Data.SqlClient;
using eShopDashboard.Infrastructure.Data.Ordering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using eShopDashboard.EntityModels.Ordering;
using Microsoft.ML.Runtime.FastTree.Internal;
using System.Linq;
using System.Text;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace eShopDashboard.Infrastructure.Setup
{
    public class OrderingContextSetup
    {
        private readonly OrderingContext _dbContext;
        private readonly ILogger<OrderingContextSetup> _logger;
        private readonly string _setupPath;

        private string[] _orderLines;
        private OrderItem[] _orderItems;
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

            var sw = new Stopwatch();
            sw.Start();

            using (TextReader reader = File.OpenText(Path.Combine(_setupPath, "OrderItems.csv")))
            {
                var csv = new CsvReader(reader);

                csv.Configuration.RegisterClassMap<OrderItemMap>();
                csv.Configuration.HeaderValidated = null;
                csv.Configuration.MissingFieldFound = null;
                csv.Configuration.TypeConverterCache.AddConverter<decimal>(new PriceConverter());

                _orderItems = csv.GetRecords<OrderItem>().ToArray();
            }

            _logger.LogDebug("----- Read {ItemCount} OrderItems ({ElapsedTime:n3}s)", _orderItems.Length, sw.Elapsed.TotalSeconds);
            //_orderItemLines = await File.ReadAllLinesAsync(Path.Combine(_setupPath, "OrderItems.sql"));

            return _orderLines.Length + _orderItems.Length;
        }

        private async Task SeedOrderItemsAsync(IProgress<int> recordsProgressHandler)
        {
            var sw = new Stopwatch();
            sw.Start();

            _logger.LogInformation("----- Seeding OrderItems");

            int i = 0;

            _dbContext.ChangeTracker.Entries().ToList().ForEach(e => e.State = EntityState.Detached);

            var sb = new StringBuilder();

            using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb; Database=eShopDashboardAI; Trusted_Connection=True; MultipleActiveResultSets=true"))
            {
                connection.Open();

                while (i < _orderItems.Length - 1)
                {
                    int j = 0;

                    sb.AppendLine("insert Ordering.OrderItems (Id,OrderId,ProductId,UnitPrice,Units,ProductName) values");

                    while (j < 1000 && i < _orderItems.Length - 1)
                    {
                        var item = _orderItems[i++];
                        j++;

                        var isLastLine = j == 1000 || i == _orderItems.Length - 1;

                        sb.AppendLine(
                            $"({item.Id},{item.OrderId},{item.ProductId},{item.UnitPrice.ToString(CultureInfo.InvariantCulture)},{item.Units},'{item.ProductName}'){(isLastLine ? ";" : ",")}");
                        //_dbContext.Add(_orderItems[i++]);
                    }

                    //sb.AppendLine("commit;");

                    var sqlCommand = new SqlCommand(sb.ToString(), connection);
                    await sqlCommand.ExecuteNonQueryAsync();

                    recordsProgressHandler.Report(i);

                    sb.Clear();

                    //await _dbContext.SaveChangesAsync();
                    //_dbContext.ChangeTracker.Entries().ToList().ForEach(e => e.State = EntityState.Detached);

                }
            }

            //var batcher = new SqlBatcher(_dbContext.Database, _logger);

            //    await batcher.ExecuteInsertSqlCommandsAsync(_orderItemLines, recordsProgressHandler, connection);

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

    public class OrderItemMap : ClassMap<OrderItem>
    {
        public OrderItemMap()
        {
            Map(oi => oi.Id).Name("Id");
            Map(oi => oi.OrderId).Name("OrderId");
            Map(oi => oi.ProductId).Name("ProductId");
            Map(oi => oi.UnitPrice).Name("UnitPrice").TypeConverter<PriceConverter>();
            Map(oi => oi.Units).Name("Units");
            Map(oi => oi.ProductName).Name("ProductName");

        }
    }

    public class PriceConverter : DecimalConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return decimal.Parse(text, CultureInfo.InvariantCulture);
        }
    }

    public class OrderItemMapper
    {

    }
}