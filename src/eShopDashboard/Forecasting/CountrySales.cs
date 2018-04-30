using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.MachineLearning;
using Microsoft.MachineLearning.Runtime;
using Microsoft.MachineLearning.Runtime.Api;
using Microsoft.MachineLearning.Runtime.Data;
using System.Threading.Tasks;

namespace eShopDashboard.Forecasting
{
    /// <summary>
    /// This is the input to the trained model.
    /// </summary>
    public class CountryData
    {
        public CountryData(string country, int year, int month, float sales, float avg, int count, float max, float min, float p_max, float p_med, float p_min, float std, float prev)
        {
            this.country = country;

            this.year = year;
            this.month = month;
            this.sales = sales;
            this.avg = avg;
            this.count = count;
            this.max = max;
            this.min = min;
            this.p_max = p_max;
            this.p_med = p_med;
            this.p_min = p_min;
            this.std = std;
            this.prev = prev;
        }

        [ColumnName("Label")]
        public float next;

        public string country;

        public float year;
        public float month;
        public float sales;
        public float avg;
        public float count;
        public float max;
        public float min;
        public float p_max;
        public float p_med;
        public float p_min;
        public float std;
        public float prev;
    }

    /// <summary>
    /// This is the output of the scored model, the prediction.
    /// </summary>
    public class CountrySalesPrediction
    {
        // Below columns are produced by the model's predictor.
        public float Score;
    }

    public class CountrySales : ICountrySales
    {
        /// <summary>
        /// This method demonstrates how to run prediction on one example at a time.
        /// </summary>
        public async Task<CountrySalesPrediction> Predict(string modelPath, string country, int year, int month, float sales, float avg, int count, float max, float min, float p_max, float p_med, float p_min, float std, float prev)
        {
            // Load model
            var predictionEngine = await CreatePredictionEngineAsync(modelPath);

            // Build country sample
            var countrySample = new CountryData(country, year, month, sales, avg, count, max, min, p_max, p_med, p_min, std, prev);

            // Returns prediction
            return predictionEngine.Predict(countrySample);
        }

        /// <summary>
        /// This function creates a prediction engine from the model located in the <paramref name="modelPath"/>.
        /// </summary>
        private async Task<PredictionModel<CountryData, CountrySalesPrediction>> CreatePredictionEngineAsync(string modelPath)
        {
            var env = new TlcEnvironment(conc: 1);
            PredictionModel<CountryData, CountrySalesPrediction> model = await PredictionModel.ReadAsync<CountryData, CountrySalesPrediction>(modelPath);
            return model;
        }
    }
}
