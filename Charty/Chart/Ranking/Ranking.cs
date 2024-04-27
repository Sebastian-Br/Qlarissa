using Charty.Chart.Analysis.ExponentialRegression;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Chart.Ranking
{
    public class Ranking
    {
        public Ranking(SymbolManager manager)
        {
            Symbols = new();
            SymbolManager = manager;
        }

        SymbolManager SymbolManager { get; set; }

        public string RankBy1YearForecast_AsText()
        {
            string result = "";
            Symbols = new();
            foreach (Symbol symbol in SymbolManager.RetrieveSymbols())
            {
                Symbols.Add(symbol);
            }

            Symbols.Sort((x, y) => y.GetNYearForecastPercent(1).CompareTo(x.GetNYearForecastPercent(1)));

            int rank = 1;
            result += ("****************************************\n");
            result += ("Symbols Ranked by Expected 1 Year Performance\n");
            result += ("****************************************\n");
            foreach (var symbol in Symbols)
            {
                result += ("Rank " + rank + ": " + symbol.ToString() + "\n");
                result += ("1YE: " + symbol.GetNYearForecastPercent(1).Round(3) + " % (Target Price: " + symbol.GetNYearForecastAbsolute(1).Round(2) + ")\n");
                rank++;
            }
            result += ("****************************************\n");

            return result;
        }

        public string RankByAggregateScore_AsText()
        {
            string result = "";
            Symbols = [.. SymbolManager.RetrieveSymbols()];

            Symbols.Sort((x, y) => GetAggregatedScore(y).CompareTo(GetAggregatedScore(x)));

            int rank = 1;
            result += ("****************************************\n");
            result += ("Symbols Ranked by Aggregate Score\n");
            result += ("****************************************\n");
            foreach (var symbol in Symbols)
            {
                result += ("Rank " + rank + ": " + symbol.ToString() + "\n");
                result += ("Score: " + GetAggregatedScore(symbol)) + "\n";
                rank++;
            }
            result += ("****************************************\n");

            return result;
        }

        public string RankBy3YearForecast_AsText()
        {
            string result = "";
            Symbols = [.. SymbolManager.RetrieveSymbols()];

            Symbols.Sort((x, y) => y.GetNYearForecastPercent(3).CompareTo(x.GetNYearForecastPercent(3)));

            int rank = 1;
            result += ("****************************************\n");
            result += ("Symbols Ranked by Expected 3 Year Performance\n");
            result += ("****************************************\n");
            foreach (var symbol in Symbols)
            {
                result += ("Rank " + rank + ": " + symbol.ToString() + "\n");
                result += ("3YE: " + symbol.GetNYearForecastPercent(3).Round(3) + " % (Target Price: " + symbol.GetNYearForecastAbsolute(3).Round(2) + ")\n");
                result += ("3YE(annualized): " + AnnualizeNYearEstimate(symbol.GetNYearForecastPercent(3), 3).Round(3) + "\n");
                rank++;
            }
            result += ("****************************************\n");

            return result;
        }

        public (double,double) GetWeightedNYearForecast(Symbol symbol, double N)
        {
            (double, double) doubleTrouble = new();
            doubleTrouble.Item1 = symbol.DataPoints.Last().Date.ToDouble() + 3.0;
            doubleTrouble.Item2 = symbol.DataPoints.Last().MediumPrice * Math.Pow(1.0 + GetWeighted1YearEquivalentForecast(symbol) / 100.0, N);
            return doubleTrouble;
        }

        private double GetWeighted1YearEquivalentForecast(Symbol symbol) // [%]
        {
            double _1YE = symbol.GetNYearForecastPercent(1);
            double _3YEa = AnnualizeNYearEstimate(symbol.GetNYearForecastPercent(3), 3); // Three-Year-annualized

            double _1YE_Weight = 1;
            double _3YEa_Weight = 2;
            double total_YE_weights = _1YE_Weight + _3YEa_Weight;

            double _1YE_weighted = _1YE * _1YE_Weight;
            double _3YEa_weighted = _3YEa * _3YEa_Weight;

            double normalized_1YE = _1YE_weighted / total_YE_weights;
            double normalized_3YEa = _3YEa_weighted / total_YE_weights;

            double weightedAnnualizedForecast = normalized_1YE + normalized_3YEa;
            return weightedAnnualizedForecast;
        }

        private double GetAggregatedScore(Symbol symbol)
        {
            double currentScore = GetWeighted1YearEquivalentForecast(symbol) * 10.0;

            double marketCap = symbol.Overview.MarketCapitalization;
            double marketCapUSDequivalent;

            if(symbol.Overview.Currency == Enums.Currency.USD)
            {
                marketCapUSDequivalent = marketCap;
            }
            else if (symbol.Overview.Currency == Enums.Currency.EUR)
            {
                marketCapUSDequivalent = marketCap * 1.08; // TODO: Get forex values automatically
            }
            else if (symbol.Overview.Currency == Enums.Currency.GBP)
            {
                marketCapUSDequivalent = marketCap * 1.26;
            }
            else if (symbol.Overview.Currency == Enums.Currency.AUD)
            {
                marketCapUSDequivalent = marketCap * 0.65;
            }
            else if (symbol.Overview.Currency == Enums.Currency.CAD)
            {
                marketCapUSDequivalent = marketCap * 0.74;
            }
            else
            {
                throw new NotImplementedException(nameof(symbol.Overview.Currency));
            }

            currentScore = currentScore * Sigmoidal_MarketCap_Weight(marketCapUSDequivalent); // prefer larger/more diversified corporations
            currentScore = currentScore * GetMaxRsquared(symbol); // prefer corporations that are more predictable/stable

            return currentScore;
        }

        private double Sigmoidal_MarketCap_Weight(double marketCap)
        {
            double k = 0.25;
            double marketCap_inBillions = marketCap / 1e9;
            return 
                (1.0) 
                /
                (1.0 + Math.Exp(-k * (marketCap_inBillions - 8)));
        }

        private double GetMaxRsquared(Symbol symbol)
        {
            double maxRsquared = 0.0;
            if(symbol.ExponentialRegressionModel.GetRsquared() > maxRsquared)
            {
                maxRsquared = symbol.ExponentialRegressionModel.GetRsquared();
            }
            if (symbol.ProjectingCAGRmodel.GetRsquared() > maxRsquared)
            {
                maxRsquared = symbol.ProjectingCAGRmodel.GetRsquared();
            }
            if (symbol.InverseLogRegressionModel.GetRsquared() > maxRsquared)
            {
                maxRsquared = symbol.InverseLogRegressionModel.GetRsquared();
            }

            return maxRsquared;
        }

        private double AnnualizeNYearEstimate(double estimate, double n)
        {
            double rate = estimate / 100.0;
            return Math.Round((Math.Pow(1.0 + rate, 1.0 / n) - 1.0) * 100.0, 6); // Rounding to 6 decimal places
        }

        List<Symbol> Symbols;
    }
}