using System;
using System.Threading.Tasks;

namespace eShopForecastModelsTrainer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await CountryModelHelper.SaveModel("data/countries.stats.csv");
                await CountryModelHelper.TestPrediction();
                await ProductModelHelper.SaveModel("data/products.stats.csv");
                await ProductModelHelper.TestPrediction();
            } catch(Exception ex)
            {
                Console.Write(ex.Message);
            }
        }
    }
}
