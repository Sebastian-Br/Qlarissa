using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Api.AlphaVantage
{
    public class ApiSymbol
    {
        public ApiSymbol()
        {
            MetaData = new();
            DataPoints = new();
        }

        [JsonProperty("Meta Data")]
        public ApiSymbolMetaData MetaData { get; set; }

        [JsonProperty("Time Series (Daily)")]
        public Dictionary<DateOnly, ApiSymbolDataPoint> DataPoints { get; set; }

        public Symbol ToBusinessChart(SymbolOverview overview)
        {
            if (overview is null)
            {
                throw new ArgumentNullException(nameof(overview));
            }

            AdjustForSplits();

            SymbolDataPoint[] dataPoints = new SymbolDataPoint[DataPoints.Count];
            for (int a = 0; a < DataPoints.Count; a++)
            {
                dataPoints[a] = new();
            }

            int i = 0;
            foreach (var item in DataPoints)
            {
                dataPoints[i].Date = item.Key;
                dataPoints[i].HighPrice = Convert.ToDouble(item.Value.high);
                dataPoints[i].LowPrice = Convert.ToDouble(item.Value.low);
                dataPoints[i].MediumPrice = (dataPoints[i].HighPrice + dataPoints[i].LowPrice) / 2.0;
                i++;
            }

            Symbol symbol = new(dataPoints, overview);
            return symbol;
        }

        public void AdjustForSplits()
        {
            

            foreach (var kvp in DataPoints)
            {
                double splitCoefficient = 1.0;
                if (kvp.Value.split_coefficient != "1.0")
                {
                    splitCoefficient = double.Parse(kvp.Value.split_coefficient);
                }
                else
                {
                    continue;
                }

                double splitCoefficientFactor = 1.0 / splitCoefficient;

                foreach (var previousKvp in DataPoints.TakeWhile(previousKvp => previousKvp.Key < kvp.Key))
                {
                    double previousHigh = double.Parse(previousKvp.Value.high);
                    double previousLow = double.Parse(previousKvp.Value.low);

                    // Adjust high and low values
                    previousKvp.Value.high = (previousHigh * splitCoefficientFactor).ToString();
                    previousKvp.Value.low = (previousLow * splitCoefficientFactor).ToString();
                }
            }
        }
    }
}