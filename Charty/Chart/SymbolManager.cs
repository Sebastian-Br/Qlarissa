using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Charty.Chart.ExcludedTimePeriods;
using Charty.Chart.Api.ApiChart;
using Charty.Chart.ChartAnalysis;
using System.Runtime.CompilerServices;

namespace Charty.Chart
{
    public class SymbolManager
    {
        public SymbolManager(IConfiguration configuration, CustomConfiguration.CustomConfiguration customConfiguration)
        {
            ApiManager = new(configuration);
            DefaultExcludedTimePeriods = customConfiguration.DefaultExcludedTimePeriods ?? throw new ArgumentNullException("DefaultExcludedTimePeriods is null");
            AlternateOverviewSource = customConfiguration.AlternateOverviewSource ?? throw new ArgumentNullException(nameof(customConfiguration.AlternateOverviewSource));
            ConfigurationSymbols = customConfiguration.SymbolsToBeAnalyzed;

            SymbolDictionary = new();
            RankByExpRegressionResult = new();
        }
        private ApiManager ApiManager {  get; set; }

        private Dictionary <string, Symbol> SymbolDictionary { get; set; }

        private Dictionary<string, ExcludedTimePeriod> DefaultExcludedTimePeriods {  get; set; }

        private RankByExpRegressionResult RankByExpRegressionResult { get; set; }

        private Dictionary<string, SymbolOverview> AlternateOverviewSource { get; set; }

        private Dictionary<string,string> ConfigurationSymbols { get; set; }

        public async Task InitializeSymbol(string symbol)
        {
            ApiSymbol apiChart = await ApiManager.GetApiSymbol(symbol);
            ApiOverview apiOverview = await ApiManager.GetApiOverview(symbol);
            SymbolOverview chartOverview;

            if(apiOverview == null || string.IsNullOrEmpty(apiOverview.Name)) // indicating that retrieving the apiOverview failed
            {
                if (AlternateOverviewSource.ContainsKey(symbol))
                {
                    chartOverview = AlternateOverviewSource[symbol];
                    Console.WriteLine("Added '" + symbol + "' from the AlternateOverviewSource");
                }
                else
                {
                    throw new InvalidOperationException("Can not get Overview data for " + symbol);
                }
            }
            else
            {
                chartOverview = apiOverview.ToBusinessOverview();
            }

            Symbol result = apiChart.ToBusinessChart(chartOverview);
            foreach (KeyValuePair<string, ExcludedTimePeriod> entry in DefaultExcludedTimePeriods)
            {
                result.AddExcludedTimePeriod(entry.Key, entry.Value);
            }

            SymbolDictionary.Add(symbol, result);
            Console.WriteLine("Added '" + symbol + "'");
        }

        public async Task AddConfigurationSymbols()
        {
            foreach(string symbol in ConfigurationSymbols.Keys)
            {
                await InitializeSymbol(symbol);
            }
        }

        public Symbol RetrieveSymbol(string symbol)
        {
            Symbol value;
            if(SymbolDictionary.TryGetValue(symbol, out value))
            {
                return value;
            }

            return null;
        }

        public bool RemoveSymbol(string symbol)
        {
            return SymbolDictionary.Remove(symbol);
        }

        public bool ContainsSymbol(string symbol)
        {
            return SymbolDictionary.ContainsKey(symbol);
        }

        public bool AnalyzeAll()
        {
            if(SymbolDictionary.Count > 0)
            {
                Console.WriteLine("Starting Analysis");
                foreach (Symbol chart in SymbolDictionary.Values)
                {
                    chart.RunExponentialRegression();
                }
                Console.WriteLine("Analysis Complete");
                return true;
            }
            else
            {
                Console.WriteLine("No symbols to analyze");
                return false;
            }
        }

        public void RankExponentialRegressionResultsBy1YearForecast()
        {
            if(!AnalyzeAll())
            {
                return;
            }

            RankByExpRegressionResult = new();
            foreach (Symbol symbol in SymbolDictionary.Values)
            {
                RankByExpRegressionResult.ExponentialRegressionResults.Add(symbol.ExponentialRegressionModel);
            }

            RankByExpRegressionResult.PrintResultsRankedBy1YearEstimate();
        }

        public void RankExponentialRegressionResultsBy3YearForecast()
        {
            if (!AnalyzeAll())
            {
                return;
            }

            RankByExpRegressionResult = new();
            foreach (Symbol symbol in SymbolDictionary.Values)
            {
                RankByExpRegressionResult.ExponentialRegressionResults.Add(symbol.ExponentialRegressionModel);
            }

            RankByExpRegressionResult.PrintResultsRankedBy3YearEstimate();
        }
    }
}