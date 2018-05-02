using eShopDashboard.EntityModels.Catalog;
using eShopDashboard.Infrastructure.Data.Catalog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace eShopDashboard.Infrastructure.Setup
{
    public class CatalogContextSetup
    {
        private readonly CatalogContext _dbContext;
        private readonly ILogger<CatalogContextSetup> _logger;
        private readonly string _setupPath;

        private string[] _dataLines;
        private SeedingStatus _status;

        public CatalogContextSetup(
            CatalogContext dbContext,
            IHostingEnvironment env,
            ILogger<CatalogContextSetup> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            _setupPath = Path.Combine(env.ContentRootPath, "Infrastructure", "Setup");
        }

        public async Task<SeedingStatus> GetSeedingStatusAsync()
        {
            if (_status != null) return _status;

            if (await _dbContext.CatalogItems.AnyAsync()) return _status = new SeedingStatus(false);

            int dataLinesCount = await GetDataToLoad();

            return _status = new SeedingStatus(dataLinesCount);
        }

        public async Task SeedAsync(IProgress<int> catalogProgressHandler)
        {
            var seedingStatus = await GetSeedingStatusAsync();

            if (!seedingStatus.NeedsSeeding) return;

            _logger.LogInformation($@"----- Seeding CatalogContext from ""{_setupPath}""");

            await SeedCatalogItemsAsync(catalogProgressHandler);
        }

        private async Task<int> GetDataToLoad()
        {
            var dataFile = Path.Combine(_setupPath, "CatalogItems.sql");

            _dataLines = await File.ReadAllLinesAsync(dataFile);

            //---------------------------------------------
            // Times 2 to account for item tags processing
            //---------------------------------------------

            return _dataLines.Length * 2;
        }

        private async Task SeedCatalogItemsAsync(IProgress<int> recordsProgressHandler)
        {
            var sw = new Stopwatch();
            sw.Start();

            var itemCount = 0;
            var tagCount = 0;

            void Aggregator ()
            {
                recordsProgressHandler.Report(itemCount + tagCount);
            };

            var itemsProgressHandler = new Progress<int>(value =>
            {
                itemCount = value;
                Aggregator();
            });

            var tagsProgressHandler = new Progress<int>(value =>
            {
                tagCount = value;
                Aggregator();
            });

            _logger.LogInformation("----- Seeding CatalogItems");

            var batcher = new SqlBatcher(_dbContext.Database, _logger);

            await batcher.ExecuteInsertCommandsAsync(_dataLines, itemsProgressHandler);

            _logger.LogInformation("----- CatalogItems Inserted ({TotalSeconds:n3}s)", sw.Elapsed.TotalSeconds);


            await SeedCatalogTagsAsync(tagsProgressHandler);
        }

        private async Task SeedCatalogTagsAsync(IProgress<int> recordsProgressHandler)
        {
            var sw = new Stopwatch();
            sw.Start();

            _logger.LogInformation("----- Adding CatalogTags");
            var tagsText = await File.ReadAllTextAsync(Path.Combine(_setupPath, "CatalogTags.json"));

            var tags = JsonConvert.DeserializeObject<List<CatalogFullTag>>(tagsText);

            _logger.LogInformation("----- Adding tags to CatalogItems");

            int i = 0;

            foreach (var tag in tags)
            {
                var entity = await _dbContext.CatalogItems.FirstOrDefaultAsync(ci => ci.Id == tag.ProductId);

                if (entity == null) continue;

                entity.TagsJson = JsonConvert.SerializeObject(tag);

                _dbContext.Update(entity);

                recordsProgressHandler.Report(++i);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"----- {i} CatalogTags added ({sw.Elapsed.TotalSeconds:n3}s)");
        }
    }
}