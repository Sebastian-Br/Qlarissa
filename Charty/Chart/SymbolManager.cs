using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Charty.Chart.ExcludedTimePeriods;
using System.Runtime.CompilerServices;
using Charty.Chart.Analysis.ExponentialRegression;
using ScottPlot;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ScottPlot.TickGenerators.TimeUnits;
using Charty.Chart.Api;
using Charty.Chart.Api.PYfinance;

namespace Charty.Chart
{
    public class SymbolManager
    {
        public SymbolManager(IConfiguration configuration, CustomConfiguration.CustomConfiguration customConfiguration)
        {
            DefaultExcludedTimePeriods = customConfiguration.DefaultExcludedTimePeriods ?? throw new ArgumentNullException("DefaultExcludedTimePeriods is null");
            ConfigurationSymbols = customConfiguration.SymbolsToBeAnalyzed;

            SymbolDictionary = new();
            RankByExpRegressionResult = new();
            DataBase = new(configuration);
            ImportSymbolDictionaryFromDataBase();
            PyFinanceAPI = new PyFinanceApiManager(configuration);
        }

        private Database.DB DataBase { get; set; }

        private IApiManager PyFinanceAPI { get; set; }

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
                else
                {
                    Console.WriteLine("Updating " + symbol);
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

            Symbol result = await PyFinanceAPI.RetrieveSymbol(symbol);
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

        public void Draw(string symbolStr)
        {
            var symbol = SymbolDictionary[symbolStr];
            SymbolDataPoint[] dataPoints = symbol.GetDataPointsNotInExcludedTimePeriods();
            double[] x = dataPoints.Select(point => ConvertDateToYearIndex(point.Date)).ToArray();
            double[] y = dataPoints.Select(point => point.MediumPrice).ToArray();
            //int numberOfDataPoints = mediumPrices.Length;

            double firstYearIndex = ConvertDateToYearIndex(dataPoints.First().Date);
            double lastYearIndex = ConvertDateToYearIndex(dataPoints.Last().Date);

            DateOnly xDateForExpRegression = dataPoints.First().Date;
            double xDateIndexForExpReg = ConvertDateToYearIndex(xDateForExpRegression);

            List<double> expRegXs = new List<double>();
            List<double> expRegYs = new List<double>();


            while(xDateIndexForExpReg <= lastYearIndex)
            {
                expRegXs.Add(xDateIndexForExpReg);
                expRegYs.Add(symbol.ExponentialRegressionModel.GetEstimate(xDateForExpRegression));

                xDateForExpRegression = xDateForExpRegression.AddDays(1);
                xDateIndexForExpReg = ConvertDateToYearIndex(xDateForExpRegression);
            }

            ScottPlot.Plot myPlot = new();
            myPlot.Add.Scatter(x, y);

            ScottPlot.Palettes.Category20 palette = new();
            var Scatter = myPlot.Add.Scatter(expRegXs.ToArray(), expRegYs.ToArray());
            Scatter.Label = "Exponential Regression A= " + symbol.ExponentialRegressionModel.A + " B=" + symbol.ExponentialRegressionModel.B;
            Scatter.Color = palette.Colors[2];
            myPlot.Title(symbolStr);
            myPlot.SavePng(symbolStr + ".png", 1200, 900);
        }

        private double ConvertDateToYearIndex(DateOnly date)
        {
            int year = date.Year;
            int dayOfYear = date.DayOfYear;
            int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
            double yearIndex = year + dayOfYear / (double)daysInYear;
            return yearIndex;
        }
    }
}