using System.Collections.Generic;
using System.Threading.Tasks;

namespace eShopDashboard.Queries
{
    public interface ICatalogQueries
    {
        Task<IEnumerable<dynamic>> GetProductsByDescriptionAsync(string description);
    }
}