using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eShopDashboard.Forecasting;
using eShopDashboard.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace eShopDashboard.Controllers
{
    [Produces("application/json")]
    [Route("api/forecasting")]
    public class ForecastingController : Controller
    {
        private readonly AppSettings appSettings;
        private readonly IProductSales productSales;
        private readonly ICountrySales countrySales;

        public ForecastingController(IOptionsSnapshot<AppSettings> appSettings, IProductSales productSales, ICountrySales countrySales)
        {
            this.appSettings = appSettings.Value;
            this.productSales = productSales;
            this.countrySales = countrySales;
        }

        [HttpGet]
        [Route("product/{productId}/unitdemandestimation")]
        public async Task <IActionResult> GetProductUnitDemandEstimation(string productId,
            [FromQuery]int year, [FromQuery]int month,
            [FromQuery]float units, [FromQuery]float avg,
            [FromQuery]int count, [FromQuery]float max,
            [FromQuery]float min, [FromQuery]float prev,
            [FromQuery]float idx)
        {
            // next,productId,year,month,units,avg,count,max,min,idx,prev
            var nextMonthUnitDemandEstimation = await productSales.Predict($"{appSettings.AIModelsPath}/product_month_fastTreeTweedle.zip", productId, year, month, units, avg, count, max, min, prev, idx);

            return Ok(nextMonthUnitDemandEstimation.Score);
        }

        [HttpGet]
        [Route("country/{country}/unitdemandestimation")]
        public async Task<IActionResult> GetCountrySalesForecast(string country,
            [FromQuery]int year,
            [FromQuery]int month, [FromQuery]float avg,
            [FromQuery]float max, [FromQuery]float min,
            [FromQuery]float prev, [FromQuery]int count,
            [FromQuery]float sales, [FromQuery]float idx)
        {
            // next,country,year,month,max,min,idx,count,units,avg,prev
            var nextMonthSalesForecast = await countrySales.Predict($"{appSettings.AIModelsPath}/country_month_fastTreeTweedle.zip", country, year, month, max, min, idx, count, sales, avg, prev);

            return Ok(nextMonthSalesForecast.Score);
        }
    }
}
