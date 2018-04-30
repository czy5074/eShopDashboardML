using eShopDashboard.Extensions;
using eShopDashboard.Infrastructure.Data.Catalog;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eShopDashboard.Queries
{
    public class CatalogQueries : ICatalogQueries
    {
        private readonly CatalogContext _context;

        public CatalogQueries(
            CatalogContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<dynamic>> GetProductsByDescriptionAsync(string description)
        {
            var itemList = await _context.CatalogItems
                .Where(c => c.Description.Contains(description))
                .Select(ci => new
                {
                    ci.Id,
                    ci.CatalogBrandId,
                    ci.Description,
                    ci.Price,
                    ci.PictureUri,
                    color = ci.Tags.Color.JoinTags(),
                    size = ci.Tags.Size.JoinTags(),
                    shape = ci.Tags.Shape.JoinTags(),
                    quantity = ci.Tags.Quantity.JoinTags(),
                    ci.Tags.agram,
                    ci.Tags.bgram,
                    ci.Tags.abgram,
                    ci.Tags.ygram,
                    ci.Tags.zgram,
                    ci.Tags.yzgram
                })
                .ToListAsync();

            return itemList;
        }
    }
}