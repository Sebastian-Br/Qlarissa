﻿using System;
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
using ScottPlot.AxisPanels;
using ScottPlot.TickGenerators;
using MathNet.Numerics;

namespace Charty.Chart
{
    public class SymbolManager
    {
        public SymbolManager(IConfiguration configuration, CustomConfiguration.CustomConfiguration customConfiguration)
        {
            DefaultExcludedTimePeriods = customConfiguration.DefaultExcludedTimePeriods ?? throw new ArgumentNullException("DefaultExcludedTimePeriods is null");
            ConfigurationSymbols = customConfiguration.SymbolsToBeAnalyzed;

            SymbolDictionary = new();
            DataBase = new(configuration);
            //ImportSymbolDictionaryFromDataBase();
            PyFinanceAPI = new PyFinanceApiManager(configuration);
            Ranking = new(this);
        }

        private Database.DB DataBase { get; set; }

        private IApiManager PyFinanceAPI { get; set; }

        private Dictionary <string, Symbol> SymbolDictionary { get; set; }

        private Dictionary<string, ExcludedTimePeriod> DefaultExcludedTimePeriods {  get; set; }

        private Dictionary<string,string> ConfigurationSymbols { get; set; }

        private Ranking.Ranking Ranking { get; set; }

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
            //result.RunRegressions_IfNotExists();

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

            return;
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

        public List<Symbol> RetrieveSymbols()
        {
            return SymbolDictionary.Values.ToList();
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
                    Console.WriteLine("Analyzing: " +  symbol);
                    symbol.RunRegressions_IfNotExists();
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

        public string RankBy1YearForecast()
        {
            return Ranking.RankBy1YearForecast_AsText();
        }
        public string RankBy3YearForecast()
        {
            return Ranking.RankBy3YearForecast_AsText();
        }

        public string RankByAggregateScore()
        {
            return Ranking.RankByAggregateScore_AsText();
        }

        public void Draw(string symbolStr)
        {
            DrawLog(symbolStr);
            return;
        }

        public void DrawAll()
        {
            foreach(string symbolStr in SymbolDictionary.Keys)
            {
                DrawLog(symbolStr);
            }

            Console.WriteLine("Drew all Symbols");
            return;
        }

        public void DrawLog(string symbolStr)
        {
            int logConstant = 2;

            var symbol = SymbolDictionary[symbolStr];
            SymbolDataPoint[] dataPoints = symbol.GetDataPointsNotInExcludedTimePeriods();
            double[] x = dataPoints.Select(point => point.Date.ToDouble()).ToArray();
            double[] y = dataPoints.Select(point => point.MediumPrice).ToArray();
            //int numberOfDataPoints = mediumPrices.Length;

            double firstYearIndex = dataPoints.First().Date.ToDouble();
            double lastYearIndex = dataPoints.Last().Date.ToDouble();

            DateOnly xDateForExpRegression = dataPoints.First().Date;
            double xDateIndexForExpReg = xDateForExpRegression.ToDouble();

            List<double> expRegXs = new();
            List<double> expRegYs = new();

            List<double> cascadingCagrXs = new();
            List<double> cascadingCagrYs = new();

            List<double> inverseLogXs = new();
            List<double> inverseLogYs = new();

            /*while (xDateIndexForExpReg <= lastYearIndex)
            {
                expRegXs.Add(xDateIndexForExpReg);
                expRegYs.Add(symbol.ExponentialRegressionModel.GetEstimate(xDateIndexForExpReg));

                cascadingCagrXs.Add(xDateIndexForExpReg);
                cascadingCagrYs.Add(symbol.CascadingCAGR.GetEstimate(xDateIndexForExpReg));

                xDateForExpRegression = xDateForExpRegression.AddDays(1);
                xDateIndexForExpReg = xDateForExpRegression.ToDouble();
            }*/

            double startYear = 2009.5;
            double endYear = DateOnly.FromDateTime(DateTime.Now.AddYears(3)).ToDouble() + 0.08;
            for(double d = startYear; d < endYear; d+= 0.01)
            {
                expRegXs.Add(d);
                expRegYs.Add(symbol.ExponentialRegressionModel.GetEstimate(d));
                cascadingCagrXs.Add(d);
                cascadingCagrYs.Add(symbol.ProjectingCAGRmodel.GetEstimate(d));
                inverseLogXs.Add(d);
                inverseLogYs.Add(symbol.InverseLogRegressionModel.GetEstimate(d));
            }

            ScottPlot.Plot myPlot = new();
            myPlot.Axes.SetLimitsX(AxisLimits.HorizontalOnly(startYear, endYear));
            //myPlot.Axes.SetLimitsY(AxisLimits.VerticalOnly(0, 1400));
            myPlot.Add.Scatter(x, y.Select(y => Math.Log(y)).ToArray()); // adds symbol x,y

            ScottPlot.Palettes.Category20 palette = new();
            var expRegScatter = myPlot.Add.Scatter(expRegXs.ToArray(), expRegYs.ToArray().Select(y => Math.Log(y)).ToArray());
            expRegScatter.Color = palette.Colors[2];
            //expRegScatter.LineWidth = 0.2f;
            expRegScatter.MarkerSize = 0.5f;
            expRegScatter.Label = "EXP";

            if ((cascadingCagrYs.ToArray().Select(y => Math.Log(y)).Any(x => x <= 0))) {
                throw new Exception("PCAGR");
            }

            if ((expRegYs.ToArray().Select(y => Math.Log(y)).Any(x => x <= 0)))
            {
                throw new Exception("EXP");
            }

            if ((y.ToArray().Select(y => Math.Log(y)).Any(x => x <= 0)))
            {
                throw new Exception("y");
            }

            var cascadingCAGRscatter = myPlot.Add.Scatter(cascadingCagrXs.ToArray(), cascadingCagrYs.ToArray().Select(y => Math.Log(y)).ToArray());
            cascadingCAGRscatter.Color = palette.Colors[6];
            cascadingCAGRscatter.MarkerSize = 0.5f;
            cascadingCAGRscatter.Label = "PCAGR";

            var inverseLogScatter = myPlot.Add.Scatter(inverseLogXs.ToArray(), inverseLogYs.ToArray().Select(y => Math.Log(y)).ToArray());
            inverseLogScatter.Color = Colors.Black;
            inverseLogScatter.MarkerSize = 0.75f;
            inverseLogScatter.Label = "INVLOG";

            // Use a custom formatter to control the label for each tick mark
            static string logTickLabels(double y) => Math.Pow(double.E, y).ToString("N0");
            // create a minor tick generator that places log-distributed minor ticks
            ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGen = new();
            ScottPlot.TickGenerators.NumericAutomatic tickGen = new();
            tickGen.MinorTickGenerator = minorTickGen;
            // create a custom tick formatter to set the label text for each tick
            static string LogTickLabelFormatter(double y) => $"{Math.Pow(double.E, y):N0}";
            //tickGen.IntegerTicksOnly = true;
            tickGen.LabelFormatter = LogTickLabelFormatter;
            myPlot.Axes.Left.TickGenerator = tickGen;

            int rsquaredDecimals = 9;
            myPlot.Title(symbol.ToString() + "\nEXPR²=" + symbol.ExponentialRegressionModel.GetRsquared().Round(rsquaredDecimals)
                + " PCAGRR²=" + symbol.ProjectingCAGRmodel.GetRsquared().Round(rsquaredDecimals)
                + " INVLOGR²=" + symbol.InverseLogRegressionModel.GetRsquared().Round(rsquaredDecimals));
            myPlot.Axes.Bottom.Label.Text = "Time [years]";
            myPlot.Axes.Left.Label.Text = "Price [" + symbol.Overview.Currency.ToString() + "]";

            myPlot.ShowLegend();
            myPlot.Legend.Orientation = Orientation.Horizontal;
            myPlot.Axes.Title.Label.OffsetY = -35;

            var verticalLine1Y = myPlot.Add.VerticalLine(DateOnly.FromDateTime(DateTime.Now.AddYears(1)).ToDouble());
            verticalLine1Y.Text = "+1Y";
            verticalLine1Y.LineWidth = 1;
            verticalLine1Y.LinePattern = LinePattern.Dotted;
            verticalLine1Y.LabelOppositeAxis = true;

            var verticalLine3Y = myPlot.Add.VerticalLine(DateOnly.FromDateTime(DateTime.Now.AddYears(3)).ToDouble());
            verticalLine3Y.Text = "+3Y";
            verticalLine3Y.LineWidth = 1;
            verticalLine3Y.LinePattern = LinePattern.Dotted;
            verticalLine3Y.LabelOppositeAxis = true;

            //myPlot.Axes.Right.TickGenerator = tickGen;
            double _1YE_weighted_X = symbol.DataPoints.Last().Date.ToDouble() + 1.0;
            double _1YE_weighted_Y = symbol.GetNYearForecastAbsolute(1.0);
            ScottPlot.Plottables.Marker marker_1YE = new()
            {
                X = _1YE_weighted_X,
                Y = Math.Log(_1YE_weighted_Y),
                Size = 8,
                Color = Colors.Purple,
                Shape = MarkerShape.OpenDiamond,
                Label = _1YE_weighted_Y.Round(2).ToString(),
                LineWidth = 2,
            };

            myPlot.Add.Plottable(marker_1YE);

            double _3YE_weighted_X = symbol.DataPoints.Last().Date.ToDouble() + 3.0;
            double _3YE_weighted_Y = symbol.GetNYearForecastAbsolute(3.0);
            ScottPlot.Plottables.Marker marker3YE = new()
            {
                X = _3YE_weighted_X,
                Y = Math.Log(_3YE_weighted_Y),
                Size = 8,
                Color = Colors.SaddleBrown,
                Shape = MarkerShape.OpenDiamond,
                Label = _3YE_weighted_Y.Round(2).ToString(),
                LineWidth = 2,
            };

            myPlot.Add.Plottable(marker3YE);

            myPlot.SavePng(symbolStr + ".png", 1250, 575);
        }
    }
}