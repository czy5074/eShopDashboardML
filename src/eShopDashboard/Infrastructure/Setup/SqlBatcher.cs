using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace eShopDashboard.Infrastructure.Setup
{
    public class SqlBatcher
    {
        private readonly DatabaseFacade _database;
        private readonly ILogger _logger;

        public SqlBatcher(
            DatabaseFacade database,
            ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        /// <summary>
        ///     Creates batches of sql inserts commands suited for fast seeding large amounts of data.
        ///
        ///     It's intended to process sql files similar to this:
        ///
        ///     insert into Ordering.Orders (Id,Address_Country,OrderDate) values
        ///     (1001,'United Kingdom','2016-12-01 08:26:00.0000000'),
        ///     (1002,'United Kingdom','2016-12-01 08:28:00.0000000'),
        ///     (1003,'United Kingdom','2016-12-01 08:34:00.0000000'),
        ///     (1004,'United Kingdom','2016-12-01 08:34:00.0000000'),
        ///     (1005,'United Kingdom','2016-12-01 08:35:00.0000000');
        ///
        ///     insert into Ordering.Orders (Id,Address_Country,OrderDate) values
        ///     (1006,'France','2016-12-01 08:45:00.0000000'),
        ///     (1007,'United Kingdom','2016-12-01 09:00:00.0000000'),
        ///     (1008,'United Kingdom','2016-12-01 09:01:00.0000000'),
        ///     (1009,'United Kingdom','2016-12-01 09:02:00.0000000'),
        ///     (1010,'United Kingdom','2016-12-01 09:09:00.0000000');
        ///
        ///     In order to submmit two insert commands to the database
        ///
        ///     SQL Server supports up to 1000 row values per insert
        ///
        /// </summary>
        /// <param name="sqlLines"></param>
        /// <returns></returns>
        public async Task ExecuteInsertCommandsAsync(string[] sqlLines, IProgress<int> progressHandler)
        {
            var sqlCommand = new StringBuilder();
            int lines = 0;

            for (int i = 0; i < sqlLines.Length; i++)
            {
                try
                {
                    if (IsInsertLine(i) && PendingCommand())
                    {
                        await ExecuteCommandAsync();
                        progressHandler.Report(i);
                    }

                    AddCommandLine(i);

                    if (IsLastLine(i))
                    {
                        await ExecuteCommandAsync();
                        progressHandler.Report(i);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"----- Exception executing sql command: \n{sqlCommand.ToString()}");

                    throw;
                }
            }

            return;

            bool IsInsertLine(int i) => sqlLines[i].StartsWith("insert", StringComparison.InvariantCultureIgnoreCase);

            bool IsLastLine(int i) => i == sqlLines.Length - 1;

            bool PendingCommand() => sqlCommand.Length > 0;

            async Task ExecuteCommandAsync()
            {
                var sw = new Stopwatch();
                sw.Start();

                await _database.ExecuteSqlCommandAsync(sqlCommand.ToString());

                _logger.LogDebug($"----- Executed {lines} lines sql command ({sw.Elapsed.TotalMilliseconds:n3}ms)");

                sqlCommand.Clear();
                lines = 0;
            }

            void AddCommandLine(int i)
            {
                if (string.IsNullOrWhiteSpace(sqlLines[i])) return;

                lines++;
                sqlCommand.AppendLine(sqlLines[i]);
            }
        }
    }
}