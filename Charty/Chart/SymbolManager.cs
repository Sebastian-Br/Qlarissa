using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Charty.Chart.ExcludedTimePeriods;
using Charty.Chart.Api.ApiChart;
using System.Runtime.CompilerServices;
using Charty.Chart.Api;
using Charty.Chart.Analysis.ExponentialRegression;
using ScottPlot;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            DataBase = new(configuration);
            ImportSymbolDictionaryFromDataBase();
        }

        private Database.DB DataBase { get; set; }

        private ApiManager ApiManager {  get; set; }

        private Dictionary <string, Symbol> SymbolDictionary { get; set; }

        private Dictionary<string, ExcludedTimePeriod> DefaultExcludedTimePeriods {  get; set; }

        private RankByExpRegressionResult RankByExpRegressionResult { get; set; }

        private Dictionary<string, SymbolOverview> AlternateOverviewSource { get; set; }

        private Dictionary<string,string> ConfigurationSymbols { get; set; }

        private void ImportSymbolDictionaryFromDataBase()
        {
            Dictionary<string, Symbol> importedDictionary = DataBase.LoadSymbolDictionary();
            foreach (var symbol in importedDictionary.Values)
            {
                Console.WriteLine("Added '" + symbol.Overview.Symbol + "' from the DB.");
                AddDefaultExcludedTimePeriodsToSymbol(symbol);
            }

            SymbolDictionary = importedDictionary;
        }

        public async Task InitializeSymbolFromAPI(string symbol, bool performUpdate = false)
        {
            if (SymbolDictionary.ContainsKey(symbol))
            {
                if(performUpdate == false)
                {
                    Console.WriteLine("Symbol '" + symbol + "' is already known.");
                    return;
                }
            }
            else
            {
                if (performUpdate) // and does not contain key
                {
                    Console.WriteLine("Symbol '" + symbol + "' can not be updated because it is unknown.");
                    return;
                }
            }

            ApiSymbol apiChart = await ApiManager.GetApiSymbol(symbol);
            ApiOverview apiOverview = await ApiManager.GetApiOverview(symbol);
            SymbolOverview overview;

            if(apiOverview == null || string.IsNullOrEmpty(apiOverview.Name)) // indicating that retrieving the apiOverview failed
            {
                if (AlternateOverviewSource.ContainsKey(symbol))
                {
                    overview = AlternateOverviewSource[symbol];
                    Console.WriteLine("Added '" + symbol + "' from the AlternateOverviewSource");
                }
                else
                {
                    throw new InvalidOperationException("Can not get Overview data for " + symbol);
                }
            }
            else
            {
                overview = apiOverview.ToBusinessOverview();
            }

            Symbol result = apiChart.ToBusinessChart(overview);
            AddDefaultExcludedTimePeriodsToSymbol(result);
            result.RunExponentialRegression_IfNotExists();

            DataBase.InsertOrUpdateSymbolInformation(result);
            //SymbolDictionary.Add(symbol, result);
            SymbolDictionary[symbol] = result;
            Console.WriteLine((performUpdate ? "Updated" : "Added") + " '" + symbol + "'");
        }

        public async Task UpdateAll()
        {
            List<string> orderedSymbolKeys = SymbolDictionary
            .OrderBy(kv => kv.Value.DataPoints.Last().Date)
            .Select(kv => kv.Key.ToUpper())
            .ToList();

            foreach (string key in orderedSymbolKeys)
            {
                await InitializeSymbolFromAPI(key, true);
            }
        }

        private void AddDefaultExcludedTimePeriodsToSymbol(Symbol symbol)
        {
            foreach (KeyValuePair<string, ExcludedTimePeriod> entry in DefaultExcludedTimePeriods)
            {
                symbol.AddExcludedTimePeriod(entry.Key, entry.Value);
            }
        }

        public async Task AddConfigurationSymbols()
        {
            foreach(string symbol in ConfigurationSymbols.Keys)
            {
                await InitializeSymbolFromAPI(symbol);
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
                foreach (Symbol symbol in SymbolDictionary.Values)
                {
                    symbol.RunExponentialRegression_IfNotExists();
                    DataBase.InsertOrUpdateSymbolInformation(symbol);
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