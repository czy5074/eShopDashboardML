using Microsoft.MachineLearning.Runtime;
using Microsoft.MachineLearning.Runtime.Api.Experiment;
using Microsoft.MachineLearning.Runtime.Api.Experiment.Categorical;
using Microsoft.MachineLearning.Runtime.Api.Experiment.TweedieFastTree;
using Microsoft.MachineLearning.Runtime.Api.Experiment.ImportTextData;
using Microsoft.MachineLearning.Runtime.Api.Experiment.ModelOperations;
using Microsoft.MachineLearning.Runtime.Api.Experiment.SchemaManipulation;
using Microsoft.MachineLearning.Runtime.Data;
using Microsoft.MachineLearning.Runtime.EntryPoints;
using System;
using System.IO;
using Microsoft.MachineLearning;
using System.Threading.Tasks;

namespace eShopForecastModelsTrainer
{
    public class CountryModelHelper
    {
        private static TlcEnvironment tlcEnvironment = new TlcEnvironment(seed: 1);
        private static IPredictorModel model;

        /// <summary>
        /// Train and save model for predicting next month country revenues
        /// </summary>
        /// <param name="dataPath">Input training file path</param>
        /// <param name="outputModelPath">Trained model path</param>
        public static void SaveModel(string dataPath, string outputModelPath = "country_month_fastTreeTweedle.zip")
        {
            if (File.Exists(outputModelPath))
            {
                File.Delete(outputModelPath);
            }

            using (var saveStream = File.OpenWrite(outputModelPath))
            {
                SaveCountryModel(dataPath, saveStream);
            }
        }

        /// <summary>
        /// Train and save model for predicting next month country revenues
        /// </summary>
        /// <param name="dataPath">Input training file path</param>
        /// <param name="stream">Trained model path</param>
        public static void SaveCountryModel(string dataPath, Stream stream)
        {
            if (model == null)
            {
                model = CreateCountryModelUsingExperiment(dataPath);
            }

            model.Save(tlcEnvironment, stream);
        }

        /// <summary>
        /// Build model for predicting next month country revenues using Experiment API
        /// </summary>
        /// <param name="dataPath">Input training file path</param>
        /// <returns></returns>
        private static IPredictorModel CreateCountryModelUsingExperiment(string dataPath)
        {
            // TlcEnvironment holds the experiment's session
            TlcEnvironment tlcEnvironment = new TlcEnvironment(seed: 1);
            Experiment experiment = tlcEnvironment.CreateExperiment();

            // First node in the workflow will be reading the source csv file, following the schema defined by dataSchema

            // This schema specifies the column name, type (TX for text or R4 for float) and column order
            // of the input training file
            var dataSchema = "col=Label:R4:0 col=country:TX:1 col=year:R4:2 col=month:R4:3 col=sales:R4:4 col=avg:R4:5 " +
                             "col=count:R4:6 col=max:R4:7 col=min:R4:8 col=p_max:R4:9 col=p_med:R4:10 col=p_min:R4:11 " +
                             "col=std:R4:12 col=prev:R4:13 " +
                             "header+ sep=,";

            var importData = new ImportText { CustomSchema = dataSchema };
            var imported = experiment.Add(importData);

            // The experiment combines columns by data types
            // First group will be made by numerical features in a vector named NumericalFeatures
            var numericalConcatenate = new ConcatColumns { Data = imported.Data };
            numericalConcatenate.AddColumn("NumericalFeatures",
                nameof(CountryData.year),
                nameof(CountryData.month),
                nameof(CountryData.sales),
                nameof(CountryData.count),
                nameof(CountryData.p_max),
                nameof(CountryData.p_med),
                nameof(CountryData.p_min),
                nameof(CountryData.std),
                nameof(CountryData.prev));
            var numericalConcatenated = experiment.Add(numericalConcatenate);

            // The second group is for categorical features, in a vecor named CategoryFeatures
            var categoryConcatenate = new ConcatColumns { Data = numericalConcatenated.OutputData };
            categoryConcatenate.AddColumn("CategoryFeatures", nameof(CountryData.country));
            var categoryConcatenated = experiment.Add(categoryConcatenate);


            var categorize = new CatTransformDict { Data = categoryConcatenated.OutputData };
            categorize.AddColumn("CategoryFeatures");
            var categorized = experiment.Add(categorize);

            // After combining columns by data type, the experiment needs all columns 
            // to be aggregated in a single column, named Features 
            var featuresConcatenate = new ConcatColumns { Data = categorized.OutputData };
            featuresConcatenate.AddColumn("Features", "NumericalFeatures", "CategoryFeatures");
            var featuresConcatenated = experiment.Add(featuresConcatenate);

            // Add the Learner to the workflow. The Learner is the machine learning algorithm used to train a model
            // In this case, we use the TweedieFastTree.TrainRegression
            var learner = new TrainRegression { TrainingData = featuresConcatenated.OutputData, NumThreads = 1 };
            var learnerOutput = experiment.Add(learner);

            // Add the Learner to the workflow. The Learner is the machine learning algorithm used to train a model
            // In this case, TweedieFastTree.TrainRegression was one of the best performing algorithms, but you can 
            // choose any other regression algorithm (StochasticDualCoordinateAscentRegressor,PoissonRegressor,...)
            var combineModels = new CombineModels
            {
                // Transformation nodes built before
                TransformModels = new ArrayVar<ITransformModel>(numericalConcatenated.Model, categoryConcatenated.Model, categorized.Model, featuresConcatenated.Model),
                // Learner
                PredictorModel = learnerOutput.PredictorModel
            };

            // Finally, add the combined model to the experiment
            var combinedModels = experiment.Add(combineModels);

            // Compile, set parameters (input files,...) and executes the experiment
            experiment.Compile();
            experiment.SetInput(importData.InputFile, new SimpleFileHandle(tlcEnvironment, dataPath, false, false));
            experiment.Run();

            // IPredictorModel is extracted from the workflow
            return experiment.GetOutput(combinedModels.PredictorModel);
        }

        /// <summary>
        /// Predict samples using saved model
        /// </summary>
        /// <param name="outputModelPath">Model file path</param>
        /// <returns></returns>
        public static async Task PredictSamples(string outputModelPath = "country_month_fastTreeTweedle.zip")
        {
            // Read the model that has been previously saved by SaveModel
            var model = await PredictionModel.ReadAsync<CountryData, CountrySalesPrediction>(outputModelPath);

            // Build sample data
            CountryData dataSample = new CountryData()
            {
                country = "United Kingdom",
                month = 10,
                year = 2017,
                avg = 506.73602F,
                p_max = 587.902F,
                p_med = 309.945F,
                p_min = 135.64000000000001F,
                max = 25035,
                min = 0.38F,
                prev = 856548.78F,
                count = 1724F,
                std = 1063.9320923325279F,
                sales = 873612.9F
            };
            // Predict sample data
            CountrySalesPrediction prediction = model.Predict(dataSample);
            Console.WriteLine($"Country: {dataSample.country}, month: {dataSample.month+1}, year: {dataSample.year} - Real value (US$): {Math.Pow(6.0084501,10)}, Forecasting (US$): {Math.Pow(prediction.Score,10)}");

            dataSample = new CountryData()
            {
                country = "United Kingdom",
                month = 11,
                year = 2017,
                avg = 427.167017F,
                p_max = 501.48800000000017F,
                p_med = 288.72F,
                p_min = 134.53600000000003F,
                max = 11351.51F,
                min = 0.42F,
                prev = 873612.9F,
                count = 2387,
                std = 707.5642048503361F,
                sales = 1019647.67F
            };
            prediction = model.Predict(dataSample);
            Console.WriteLine($"Country: {dataSample.country}, month: {dataSample.month + 1}, year: {dataSample.year} - Forecasting (US$):{Math.Pow(prediction.Score,10)}");

            dataSample = new CountryData()
            {
                country = "United States",
                month = 10,
                year = 2017,
                avg = 532.256F,
                p_max = 573.6299999999998F,
                p_med = 400.17F,
                p_min = 340.39599999999996F,
                max = 1463.87F,
                min = 281.66F,
                prev = 4264.94F,
                count = 10,
                std = 338.2866742039953F,
                sales = 5322.56F
            };
            prediction = model.Predict(dataSample);
            Console.WriteLine($"Country: {dataSample.country}, month: {dataSample.month + 1}, year: {dataSample.year} - Real value (US$): {Math.Pow(3.8057699,10)}, Forecasting (US$): {Math.Pow(prediction.Score,10)}");

            dataSample = new CountryData()
            {
                country = "United States",
                month = 11,
                year = 2017,
                avg = 581.26909F,
                p_max = 1135.99F,
                p_med = 317.9F,
                p_min = 249.44F,
                max = 1252.57F,
                min = 171.6F,
                prev = 5322.56F,
                count = 11,
                std = 409.75528400729723F,
                sales = 6393.96F
            };
            prediction = model.Predict(dataSample);
            Console.WriteLine($"Country: {dataSample.country}, month: {dataSample.month + 1}, year: {dataSample.year} - Forecasting (US$):  {Math.Pow(prediction.Score,10)}");
        }
    }
}
