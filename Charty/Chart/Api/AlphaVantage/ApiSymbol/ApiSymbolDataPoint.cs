using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Api.AlphaVantage
{
    public class ApiSymbolDataPoint
    {
        public ApiSymbolDataPoint()
        {
            high = "";
            low = "";
            split_coefficient = "";
        }

        /*[JsonProperty("1. open")]
        public string open { get; set; }*/

        [JsonProperty("2. high")]
        public string high { get; set; }

        [JsonProperty("3. low")]
        public string low { get; set; }

        /*[JsonProperty("4. close")]
        public string close { get; set; }*/

        /*[JsonProperty("5. volume")]
        public string volume { get; set; }*/

        [JsonProperty("8. split coefficient")]
        public string split_coefficient { get; set; }
    }
}