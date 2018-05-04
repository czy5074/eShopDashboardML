using System.Threading.Tasks;

namespace eShopDashboard.Forecast
{
    public interface ICountrySales
    {
        Task<CountrySalesPrediction> Predict(string modelPath, string country, int year, int month, float max, float min, float idx, int count, float units, float avg, float prev);
    }
}