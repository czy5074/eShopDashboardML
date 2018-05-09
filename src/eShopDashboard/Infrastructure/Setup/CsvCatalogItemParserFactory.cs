using System;
using eShopDashboard.EntityModels.Catalog;
using TinyCsvParser;
using TinyCsvParser.Mapping;
using TinyCsvParser.Tokenizer.RFC4180;
using TinyCsvParser.TypeConverter;

namespace eShopDashboard.Infrastructure.Setup
{
	public class CsvCatalogItemParserFactory
	{
        // Id,CatalogBrandId,CatalogTypeId,Name,Description,Price,AvailableStock,MaxStockThreshold,OnReorder,RestockThreshold
        public static string[] HeaderColumns => new[]
		{
			"Id",
			"AvailableStock",
            "CatalogBrandId",
            "CatalogTypeId",
            "Description",
			"MaxStockThreshold",
			"Name",
			"OnReorder",
			"Price",
			"RestockThreshold",
		};

		public static CsvParser<CatalogItem> CreateParser()
		{
			var tokenizerOptions = new Options('"', '\\', ',');
			var tokenizer = new RFC4180Tokenizer(tokenizerOptions);
			var parserOptions = new CsvParserOptions(true, tokenizer);
			var mapper = new CsvCatalogItemMapper();
			var parser = new CsvParser<CatalogItem>(parserOptions, mapper);

			return parser;
		}

		private class CsvCatalogItemMapper : CsvMapping<CatalogItem>
		{
			public CsvCatalogItemMapper()
			{
				MapProperty(0, m => m.Id);
                MapProperty(1, m => m.CatalogBrandId);
                MapProperty(2, m => m.CatalogTypeId);
                MapProperty(3, m => m.Name);
                MapProperty(4, m => m.Description);
                MapProperty(5, m => m.Price);
                MapProperty(6, m => m.AvailableStock);
				MapProperty(7, m => m.MaxStockThreshold);
				MapProperty(8, m => m.OnReorder, new BoolConverter("1", "0", StringComparison.InvariantCulture));
				MapProperty(9, m => m.RestockThreshold);
			}
		}
	}
}