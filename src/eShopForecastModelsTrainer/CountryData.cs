using Microsoft.ML.Runtime.Api;

namespace eShopForecastModelsTrainer
{
    public class CountryData
    {
        // next,country,year,month,max,min,idx,count,units,avg,prev
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
        public float idx;
        public float prev;
    }

    public class CountrySalesPrediction
    {
        public float Score;
    }
}
