using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Api.ApiChart
{
    public class ApiSymbol
    {
        public ApiSymbol()
        {
            MetaData = new();
            DataPoints = new();
        }

        [JsonProperty("Meta Data")]
        public ApiChartMetaData MetaData { get; set; }

        [JsonProperty("Time Series (Daily)")]
        public Dictionary<DateOnly, ApiChartDataPoint> DataPoints { get; set; }

        public Symbol ToBusinessChart(SymbolOverview chartOverview)
        {
            if(chartOverview is null)
            {
                throw new ArgumentNullException(nameof(chartOverview));
            }

            ChartDataPoint[] dataPoints = new ChartDataPoint[DataPoints.Count];
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

            Symbol chart = new(dataPoints, chartOverview);
            chart.ChartDataPoints[0].DayIndex = 1;

            for (int c = 1; c < chart.ChartDataPoints.Count(); c++)
            {
                chart.ChartDataPoints[c].DayIndex = chart.ChartDataPoints[c].Date.DayNumber - chart.ChartDataPoints[0].Date.DayNumber;
            }

            return chart;
        }
    }
}