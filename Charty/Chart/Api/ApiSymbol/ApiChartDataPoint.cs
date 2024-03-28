using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Api.ApiChart
{
    public class ApiChartDataPoint
    {
        public ApiChartDataPoint()
        {
            open = "";
            high = "";
            low = "";
            close = "";
            volume = "";
        }

        [JsonProperty("1. open")]
        public string open { get; set; }

        [JsonProperty("2. high")]
        public string high { get; set; }

        [JsonProperty("3. low")]
        public string low { get; set; }

        [JsonProperty("4. close")]
        public string close { get; set; }

        [JsonProperty("5. volume")]
        public string volume { get; set; }
    }
}