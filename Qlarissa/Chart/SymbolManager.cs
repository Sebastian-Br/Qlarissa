using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Qlarissa.Chart.ExcludedTimePeriods;
using System.Runtime.CompilerServices;
using Qlarissa.Chart.Analysis.ExponentialRegression;
using ScottPlot;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ScottPlot.TickGenerators.TimeUnits;
using Qlarissa.Chart.Api;
using Qlarissa.Chart.Api.PYfinance;
using ScottPlot.AxisPanels;
using ScottPlot.TickGenerators;
using MathNet.Numerics;
using Qlarissa.CustomConfiguration;
using Qlarissa.Chart.Enums;

namespace Qlarissa.Chart;

public class SymbolManager
{
    public SymbolManager(IConfiguration configuration, CustomConfiguration.CustomConfiguration customConfiguration)
    {
        DefaultTimePeriodsExcludedFromAnalysis = customConfiguration.DefaultTimePeriodsExcludedFromAnalysis ?? throw new ArgumentNullException("DefaultTimePeriodsExcludedFromAnalysis is null");
        DefaultTimePeriodsExcludedFromPredictionTargets = customConfiguration.DefaultExcludedTimePeriodsForPredictionTargets ?? throw new ArgumentNullException("DefaultExcludedTimePeriodsForPredictionTargets is null");
        ConfigurationSymbols = customConfiguration.SymbolsToBeAnalyzed;
        CustomConfiguration = customConfiguration;

        SymbolDictionary = new();
        PyFinanceAPI = new PyFinanceApiManager(configuration, customConfiguration);
        Ranking = new(this);
    }

    private CustomConfiguration.CustomConfiguration CustomConfiguration { get; set; }

    private IApiManager PyFinanceAPI { get; set; }

    private Dictionary <string, Symbol> SymbolDictionary { get; set; }

    private Dictionary<string, ExcludedTimePeriod> DefaultTimePeriodsExcludedFromAnalysis {  get; set; }
    private Dictionary<string, ExcludedTimePeriod> DefaultTimePeriodsExcludedFromPredictionTargets {  get; set; }

    private Dictionary<string,string> ConfigurationSymbols { get; set; }

    private Ranking.Ranking Ranking { get; set; }

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

        //SymbolDictionary.Add(symbol, result);
        SymbolDictionary[symbol] = result;
        Console.WriteLine((performUpdate ? "Updated" : "Added") + " '" + symbol + "'");
    }

    private void AddDefaultExcludedTimePeriodsToSymbol(Symbol symbol)
    {
        foreach (KeyValuePair<string, ExcludedTimePeriod> entry in DefaultTimePeriodsExcludedFromAnalysis)
        {
            symbol.AddTimePeriodExcludedFromAnalysis(entry.Key, entry.Value);
        }

        foreach (KeyValuePair<string, ExcludedTimePeriod> entry in DefaultTimePeriodsExcludedFromPredictionTargets)
        {
            symbol.AddTimePeriodExcludedFromPredictionTargets(entry.Key, entry.Value);
        }
    }

    public async Task AddConfigurationSymbols()
    {
        foreach(string symbol in ConfigurationSymbols.Keys)
        {
            bool symbolInitializedSuccessfully = false;
            do
            {
                try
                {
                    await InitializeSymbolFromAPI(symbol);
                    symbolInitializedSuccessfully = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't initialize " + symbol + ". Error message: " + ex.Message + "\nRetrying.");
                }
            }
            while (!symbolInitializedSuccessfully);
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
        Symbol symbol = SymbolDictionary[symbolStr];
        DrawSymbolChartWithRegressions_LogScale(symbol);
        if(symbol.GVA_2Years != null)
        {
            symbol.GVA_2Years.Draw();
        }

        if(symbol.GVA_1Year != null)
        {
            symbol.GVA_1Year.Draw();
        }
    }

    public void DrawAll()
    {
        foreach(string symbol in SymbolDictionary.Keys)
        {
            Console.WriteLine("Drawing: " + symbol);
            Draw(symbol);
        }

        Console.WriteLine("Drew all Symbols");
        return;
    }

    private void DrawSymbolChartWithRegressions_LogScale(Symbol symbol)
    {
        SymbolDataPoint[] dataPoints = symbol.GetDataPointsForAnalysis();
        double[] x = dataPoints.Select(point => point.Date.ToDouble()).ToArray();
        double[] y = dataPoints.Select(point => point.MediumPrice).ToArray();
        //int numberOfDataPoints = mediumPrices.Length;

        double firstYearIndex = dataPoints.First().Date.ToDouble();
        double lastYearIndex = dataPoints.Last().Date.ToDouble();

        DateOnly xDateForExpRegression = dataPoints.First().Date;
        double xDateIndexForExpReg = xDateForExpRegression.ToDouble();

        List<double> regressionFunctions_Xs = new();
        List<double> expRegYs = new();

        List<double> inverseLogYs = new();

        double startYear = symbol.DataPoints[0].Date.ToDouble();
        double endYear = DateOnly.FromDateTime(DateTime.Now.AddYears(3)).ToDouble() + 0.08;
        for(double t_time = startYear; t_time < endYear; t_time+= 0.01)
        {
            regressionFunctions_Xs.Add(t_time);
            expRegYs.Add(symbol.ExponentialRegressionModel.GetEstimate(t_time));
            inverseLogYs.Add(symbol.InverseLogRegressionModel.GetEstimate(t_time));
        }

        ScottPlot.Plot myPlot = new();
        myPlot.Axes.SetLimitsX(AxisLimits.HorizontalOnly(startYear, endYear));

        foreach(ExcludedTimePeriod excludedTimePeriod in symbol.GetTimePeriodsExcludedFromAnalysis().Values)
        {
            if(excludedTimePeriod.StartDate == null)
            {
                double left = startYear;
                double right = excludedTimePeriod.EndDate.Value.ToDouble();
                var hSpan = myPlot.Add.HorizontalSpan(left, right);
                hSpan.FillStyle.Color = Colors.LightSkyBlue.WithAlpha(.2);
            }
            else if (excludedTimePeriod.EndDate == null)
            {
                double left = excludedTimePeriod.StartDate.Value.ToDouble();
                double right = endYear;
                var hSpan = myPlot.Add.HorizontalSpan(left, right);
                hSpan.FillStyle.Color = Colors.LightSkyBlue.WithAlpha(.2);
            }
            else
            {
                double left = excludedTimePeriod.StartDate.Value.ToDouble();
                double right = excludedTimePeriod.EndDate.Value.ToDouble();
                var hSpan = myPlot.Add.HorizontalSpan(left, right);
                hSpan.FillStyle.Color = Colors.LightSkyBlue.WithAlpha(.2);
            }
        }

        var chart = myPlot.Add.Scatter(x, y.Select(y => Math.Log(y)).ToArray()); // adds symbol x,y
        chart.Color = Colors.DarkSlateGray;

        ScottPlot.Palettes.Category20 palette = new();
        var expRegScatter = myPlot.Add.Scatter(regressionFunctions_Xs.ToArray(), expRegYs.ToArray().Select(y => Math.Log(y)).ToArray());
        expRegScatter.Color = palette.Colors[2];
        expRegScatter.MarkerSize = 0.5f;
        expRegScatter.LegendText = "EXP";

        if ((expRegYs.ToArray().Any(x => x < 0)))
        {
            throw new Exception("EXP Reg Estimate < 0");
        }

        if ((y.ToArray().Any(x => x < 0)))
        {
            throw new Exception("y < 0");
        }

        var inverseLogScatter = myPlot.Add.Scatter(regressionFunctions_Xs.ToArray(), inverseLogYs.ToArray().Select(y => Math.Log(y)).ToArray());
        inverseLogScatter.Color = Colors.Black;
        inverseLogScatter.MarkerSize = 0.75f;
        inverseLogScatter.LegendText = "INVLOG";

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
            + " INVLOGR²=" + symbol.InverseLogRegressionModel.GetRsquared().Round(rsquaredDecimals));
        myPlot.Axes.Bottom.Label.Text = "Time [years]";
        myPlot.Axes.Left.Label.Text = "Price [" + symbol.Overview.Currency.ToString() + "]";

        myPlot.ShowLegend();
        myPlot.Legend.Orientation = Orientation.Horizontal;
        //myPlot.Axes.Title.Label.OffsetY = -35;

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

        double _1YE_weighted_X = symbol.DataPoints.Last().Date.ToDouble() + 1.0;
        double _1YE_weighted_Y = symbol.GetNYearForecastAbsolute(1.0);
        ScottPlot.Plottables.Marker marker_1YE = new()
        {
            X = _1YE_weighted_X,
            Y = Math.Log(_1YE_weighted_Y),
            Size = 8,
            Color = Colors.Purple,
            Shape = MarkerShape.OpenDiamond,
            LegendText = _1YE_weighted_Y.Round(2).ToString(),
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
            LegendText = _3YE_weighted_Y.Round(2).ToString(),
            LineWidth = 2,
        };

        myPlot.Add.Plottable(marker3YE);

        myPlot.SavePng(SaveLocationsConfiguration.GetSymbolChartSaveFileLocation(symbol), 1380, 600);
    }
}