using Charty.Chart.Analysis.ExponentialRegression;
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
                result += ("1YE: " + symbol.GetNYearForecastPercent(1) + " % (Target Price: " + symbol.GetNYearForecastAbsolute(1) + ")\n");
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
                result += ("3YE: " + symbol.GetNYearForecastPercent(3) + " % (Target Price: " + symbol.GetNYearForecastAbsolute(3) + ")\n");
                result += ("3YE(annualized): " + AnnualizeNYearEstimate(symbol.GetNYearForecastPercent(3), 3) + "\n");
                rank++;
            }
            result += ("****************************************\n");

            return result;
        }

        private double AnnualizeNYearEstimate(double estimate, double n)
        {
            double rate = estimate / 100.0;
            return Math.Round((Math.Pow(1.0 + rate, 1.0 / n) - 1.0) * 100.0, 6); // Rounding to 6 decimal places
        }

        List<Symbol> Symbols;
    }
}