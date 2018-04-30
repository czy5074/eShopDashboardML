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
    public class ProductData
    {
        public ProductData(string productId, int year, int month, float units, float avg, int count, float max, float min, float prev,
            float price, string color, string size, string shape, string agram, string bgram, string ygram, string zgram)
        {
            this.productId = productId;
            this.year = year;
            this.month = month;
            this.units = units;
            this.avg = avg;
            this.count = count;
            this.max = max;
            this.min = min;
            this.prev = prev;
            this.price = price;

            this.color = color;
            this.size = size;
            this.shape = shape;
            this.agram = agram;
            this.bgram = bgram;
            this.ygram = ygram;
            this.zgram = zgram;
        }

        [ColumnName("Label")]
        public float next;

        public string productId;
        public float year;
        public float month;
        public float units;
        public float avg;
        public float count;
        public float max;
        public float min;
        public float prev;
        public float price;
        public string color;
        public string size;
        public string shape;
        public string agram;
        public string bgram;
        public string ygram;
        public string zgram;
    }

    /// <summary>
    /// This is the output of the scored model, the prediction.
    /// </summary>
    public class ProductUnitPrediction
    {
        // Below columns are produced by the model's predictor.
        public float Score;
    }

    public class ProductSales : IProductSales
    {
        /// <summary>
        /// This method demonstrates how to run prediction on one example at a time.
        /// </summary>
        public async Task<ProductUnitPrediction> Predict(string modelPath, string productId, int year, int month, float units, float avg, int count, float max, float min, float prev,
            float price, string color, string size, string shape, string agram, string bgram, string ygram, string zgram)
        {
            // Load model
            var predictionEngine = await CreatePredictionEngineAsync(modelPath);

            // Build country sample
            var inputExample = new ProductData(productId, year, month, units, avg, count, max, min, prev, price, color, size, shape, agram, bgram, ygram, zgram);

            // Returns prediction
            return predictionEngine.Predict(inputExample);
        }

        /// <summary>
        /// This function creates a prediction engine from the model located in the <paramref name="modelPath"/>.
        /// </summary>
        private async Task<PredictionModel<ProductData, ProductUnitPrediction>> CreatePredictionEngineAsync(string modelPath)
        {
            var env = new TlcEnvironment(conc: 1);
            PredictionModel<ProductData, ProductUnitPrediction> model = await PredictionModel.ReadAsync<ProductData, ProductUnitPrediction>(modelPath);
            return model;
        }
    }
}
