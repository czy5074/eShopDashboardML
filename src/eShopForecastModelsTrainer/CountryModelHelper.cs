using ML = Microsoft.ML;
using Microsoft.ML.Runtime;
using Microsoft.ML.Runtime.Data;
using Microsoft.ML.Runtime.EntryPoints;
using System;
using System.IO;
using Microsoft.ML;
using System.Threading.Tasks;

namespace eShopForecastModelsTrainer
{
    public class CountryModelHelper
    {
        /// <summary>
        /// Train and save model for predicting next month country unit sales
        /// </summary>
        /// <param name="dataPath">Input training file path</param>
        /// <param name="outputModelPath">Trained model path</param>
        public static async Task SaveModel(string dataPath, string outputModelPath = "country_month_fastTreeTweedie.zip")
        {
            if (File.Exists(outputModelPath))
            {
                File.Delete(outputModelPath);
            }

            var model = CreateCountryModelUsingPipeline(dataPath);

            await model.WriteAsync(outputModelPath);
        }

        /// <summary>
        /// Build model for predicting next month country unit sales using Learning Pipelines API
        /// </summary>
        /// <param name="dataPath">Input training file path</param>
        /// <returns></returns>
        private static PredictionModel<CountryData, CountrySalesPrediction> CreateCountryModelUsingPipeline(string dataPath)
        {
            Console.WriteLine("*************************************************");
            Console.WriteLine("Training country forecasting model using Pipeline");

            var learningPipeline = new LearningPipeline();

            // First node in the workflow will be reading the source csv file, following the schema defined by dataSchema
            learningPipeline.Add(new TextLoader<CountryData>(dataPath, header: true, sep: ","));

            // The model needs the columns to be arranged into a single column of numeric type
            // First, we group all numeric columns into a single array named NumericalFeatures
            learningPipeline.Add(new ML.Transforms.ColumnConcatenator(
                outputColumn: "NumericalFeatures",
                nameof(CountryData.year),
                nameof(CountryData.month),
                nameof(CountryData.max),
                nameof(CountryData.min),
                nameof(CountryData.idx),
                nameof(CountryData.count),
                nameof(CountryData.sales),
                nameof(CountryData.avg),
                nameof(CountryData.prev)
            ));

            // Second group is for categorical features (just one in this case), we name this column CategoryFeatures
            learningPipeline.Add(new ML.Transforms.ColumnConcatenator(outputColumn: "CategoryFeatures", nameof(CountryData.country)));

            // Then we need to transform the category column using one-hot encoding. This will return a numeric array
            learningPipeline.Add(new ML.Transforms.CategoricalOneHotVectorizer("CategoryFeatures"));

            // Once all columns are numeric types, all columns will be combined
            // into a single column, named Features 
            learningPipeline.Add(new ML.Transforms.ColumnConcatenator(outputColumn: "Features", "NumericalFeatures", "CategoryFeatures"));

            // Add the Learner to the pipeline. The Learner is the machine learning algorithm used to train a model
            // In this case, TweedieFastTree.TrainRegression was one of the best performing algorithms, but you can 
            // choose any other regression algorithm (StochasticDualCoordinateAscentRegressor,PoissonRegressor,...)
            learningPipeline.Add(new ML.Trainers.FastTreeTweedieRegressor { NumThreads = 1, FeatureColumn = "Features" });

            // Finally, we train the pipeline using the training dataset set at the first stage
            var model = learningPipeline.Train<CountryData, CountrySalesPrediction>();

            return model;
        }

        /// <summary>
        /// Predict samples using saved model
        /// </summary>
        /// <param name="outputModelPath">Model file path</param>
        /// <returns></returns>
        public static async Task TestPrediction(string outputModelPath = "country_month_fastTreeTweedie.zip")
        {
            Console.WriteLine("*********************************");
            Console.WriteLine("Testing country forecasting model");

            // Read the model that has been previously saved by the method SaveModel
            var model = await PredictionModel.ReadAsync<CountryData, CountrySalesPrediction>(outputModelPath);

            // Build sample data
            var dataSample = new CountryData()
            {
                country = "17", // Netherlands
                month = 9,
                year = 2017,
                avg = 286.095F,
                max = 487.2F,
                min = 121.35F,
                idx = 33,
                prev = 8053.95F,
                count = 30,
                sales = 8582.85F
            };
            // Predict sample data
            var prediction = model.Predict(dataSample);
            Console.WriteLine($"Country: Netherlands, month: {dataSample.month + 1}, year: {dataSample.year} - Real value (US$): 5202.9, Forecasting (US$): {prediction.Score}");

            dataSample = new CountryData()
            {
                country = "17", // Netherlands
                month = 10,
                year = 2017,
                avg = 216.7875F,
                max = 384.15F,
                min = 103.35F,
                idx = 34,
                prev = 8582.85F,
                count = 24,
                sales = 5202.9F,
            };
            prediction = model.Predict(dataSample);
            Console.WriteLine($"Country: Netherlands, month: {dataSample.month + 1}, year: {dataSample.year} - Forecasting (US$):  {prediction.Score}");

            dataSample = new CountryData()
            {
                country = "33", // United States
                month = 9,
                year = 2017,
                avg = 1405.153043F,
                max = 1935.91F,
                min = 821.94F,
                idx = 33,
                prev = 42396.42F,
                count = 23,
                sales = 32318.52F
            };
            prediction = model.Predict(dataSample);
            Console.WriteLine($"Country: United States, month: {dataSample.month + 1}, year: {dataSample.year} - Real value (US$): 21373.14, Forecasting (US$): {prediction.Score}");

            dataSample = new CountryData()
            {
                country = "33", // United States
                month = 10,
                year = 2017,
                avg = 1335.82125F,
                max = 1925.01F,
                min = 780.36F,
                idx = 34,
                prev = 32318.52F,
                count = 16,
                sales = 21373.14F,
            };
            prediction = model.Predict(dataSample);
            Console.WriteLine($"Country: United States, month: {dataSample.month + 1}, year: {dataSample.year} - Forecasting (US$):  {prediction.Score}");
        }
    }
}
