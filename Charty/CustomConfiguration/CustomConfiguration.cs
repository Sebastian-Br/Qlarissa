﻿using Charty.Chart;
using Charty.Chart.ExcludedTimePeriods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.CustomConfiguration
{
    public class CustomConfiguration
    {
        public CustomConfiguration()
        {
            _httpClient = new HttpClient();
            SaveDirectoriesConfig = new();
        }

        public Dictionary<string, ExcludedTimePeriod> DefaultExcludedTimePeriods { get; set; }

        /// <summary>
        /// Keys = Symbols
        /// Values = Company names (just make the json easier to understand)
        /// </summary>
        public Dictionary<string, string> SymbolsToBeAnalyzed { get; set; }

        public SaveDirectoriesConfiguration SaveDirectoriesConfig { get; private set; }

        private HttpClient _httpClient;

        public async Task CheckSymbolsToBeAnalyzed()
        {
            Console.WriteLine("Checking if symbols in the customConfiguration.json exist");
            foreach(string symbol in SymbolsToBeAnalyzed.Keys)
            {
                if (symbol.Contains("ETR:"))
                {
                    continue; // yahoo doesn't understand this notation
                }
                string requestUri = "https://finance.yahoo.com/quote/" + symbol;
                Console.WriteLine("CheckSymbolsToBeAnalyzed(): requestUri = " + requestUri);
                HttpResponseMessage httpResponse = await _httpClient.GetAsync(requestUri);
                string response = await httpResponse.Content.ReadAsStringAsync();
                if(response.Contains("<span>No results for", StringComparison.InvariantCultureIgnoreCase) ||
                    response.Contains("<span>Symbols similar to", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new InvalidDataException("Symbol " + symbol + " is invalid.");
                }
            }

            Console.WriteLine("Success. All symbols in the customConfiguration.json exist");
        }
    }
}