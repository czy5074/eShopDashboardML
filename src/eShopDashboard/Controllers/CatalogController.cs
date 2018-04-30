using eShopDashboard.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eShopDashboard.Controllers
{
    [Produces("application/json")]
    [Route("api/catalog")]
    public class CatalogController : Controller
    {
        private readonly ICatalogQueries _queries;

        public CatalogController(ICatalogQueries queries)
        {
            _queries = queries;
        }

        // GET: api/Catalog
        [HttpGet("productSetDetailsByDescription")]
        public async Task<IActionResult> SimilarProducts([FromQuery]string description)
        {
            if (string.IsNullOrEmpty(description))
                return BadRequest();

            IEnumerable<dynamic> items = await _queries.GetProductsByDescriptionAsync(description);

            if (!items.Any()) return Ok();

            return Ok(items);
        }
    }
}