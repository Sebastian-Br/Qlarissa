using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Api.ApiChart
{
    internal class ApiManager
    {
        public ApiManager(IConfiguration configuration)
        {
            ApiClient = new();
            ApiKeys = configuration.GetRequiredSection("AlphaVantageApiKeys").Get<List<string>>();
            ApiKeyIndex = 0;

            if (ApiKeys is null)
            {
                throw new ArgumentNullException(nameof(ApiKeys));
            }

            if(ApiKeys.Count == 0)
            {
                throw new ArgumentException(nameof(ApiKeys));
            }

            foreach(string keys in ApiKeys)
            {
                if (string.IsNullOrEmpty(keys))
                {
                    throw new ArgumentException("An AlphaVantage API key is empty");
                }
            }
        }

        private HttpClient ApiClient { get; set; }

        private List<string> ApiKeys { get; set; }

        /// <summary>
        /// One key can only be used for 25 API calls each day.
        /// The index is used to point at the currently used API key within the ApiKeys list.
        /// </summary>
        private int ApiKeyIndex { get; set; }

        /// <summary>
        /// This function would normally extract data points in descending order by date. This is unintentional.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<ApiSymbol> GetApiSymbol(string symbol)
        {
            functionStart:
            string requestUri = "https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&outputsize=full&symbol=" + symbol;
            int index = ApiKeyIndex % ApiKeys.Count; // just to ensure that, even if the index is incremented while a request is running, the index will not be out of bounds
            requestUri = requestUri + "&apikey=" + ApiKeys[index];
            Console.WriteLine("GetApiSymbol(): requestUri = " + requestUri);

            HttpResponseMessage response = await ApiClient.GetAsync(requestUri);
            string jsonResponse = await response.Content.ReadAsStringAsync();

            if (jsonResponse.Contains("Error Message", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("Symbol '" + symbol + "' does not exist");
            }

            if (jsonResponse.Contains("rate limit", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("API Key with index " + ApiKeyIndex + " is exhausted. Using the next Key.");
                ApiKeyIndex++;
                if(ApiKeyIndex >= ApiKeys.Count)
                {
                    throw new ArgumentException("All API keys are exhausted");
                }
                goto functionStart;
            }

            //Console.WriteLine(jsonResponse);
            ApiSymbol apiChart = JsonConvert.DeserializeObject<ApiSymbol>(jsonResponse);
            apiChart.DataPoints = apiChart.DataPoints.Reverse().ToDictionary();
            return apiChart;
        }

        public async Task<ApiOverview> GetApiOverview(string symbol)
        {
            functionStart:
            string requestUri = "https://www.alphavantage.co/query?function=OVERVIEW&symbol=" + symbol;
            int index = ApiKeyIndex % ApiKeys.Count; // just to ensure that, even if the index is incremented while a request is running, the index will not be out of bounds
            requestUri = requestUri + "&apikey=" + ApiKeys[index];
            Console.WriteLine("GetApiOverview(): requestUri = " + requestUri);

            HttpResponseMessage response = await ApiClient.GetAsync(requestUri);
            string jsonResponse = await response.Content.ReadAsStringAsync();

            if (jsonResponse.Contains("Error Message", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("Symbol '" + symbol + "' does not exist");
            }

            if (jsonResponse.Contains("rate limit", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("API Key with index " + ApiKeyIndex + " is exhausted. Using the next Key.");
                ApiKeyIndex++;
                if (ApiKeyIndex >= ApiKeys.Count)
                {
                    throw new ArgumentException("All API keys are exhausted");
                }
                goto functionStart;
            }

            if(jsonResponse == "{}") // no Overview found
            {
                Console.WriteLine("API Overview data for " + symbol + " does not exist");
            }

            //Console.WriteLine(jsonResponse);
            ApiOverview apiOverview = JsonConvert.DeserializeObject<ApiOverview>(jsonResponse);
            return apiOverview;
        }
    }
}