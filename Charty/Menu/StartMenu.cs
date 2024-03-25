using Charty.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.Menu
{
    public class StartMenu : IMenu
    {
        public SymbolManager ChartManager { get; set; }

        public StartMenu(SymbolManager chartManager)
        {
            ChartManager = chartManager;
            PrintNameAndMenu();
        }

        public void PrintNameAndMenu()
        {
            Console.WriteLine(StateName());
            Console.WriteLine(HelpMenu());
        }

        public string HelpMenu()
        {
            return "R - Reload Menu Help\n" +
                "Add SYMBOL - Adds the Symbol to the Chart Dictionary\n" +
                "Switch SYMBOL - Switch to the Symbol's Menu\n" +
                "Remove SYMBOL - Removes the Symbol from the Chart Dictionary\n" +
                "AnalyzeAll - Run analyses (Exponential Regression) on all Symbols\n" +
                "Rank1Year - Ranks all Symbols according to their expected 1-year performance\n" +
                "Rank3Year - Ranks all Symbols according to their expected 3-year performance\n";
        }

        public string StateName()
        {
            return "\nStart Menu.\n";
        }

        public async Task<IMenu> SendText(string text)
        {
            StringComparison comparer = StringComparison.InvariantCultureIgnoreCase;
            if(string.IsNullOrEmpty(text))
            {
                return this;
            }

            if(string.Equals(text, "R"))
            {
                Console.WriteLine(HelpMenu());
                return this;
            }

            if(text.StartsWith("Add ", comparer))
            {
                string symbol = text.Replace("Add ", "").Trim();
                if(string.IsNullOrEmpty (symbol))
                {
                    Console.WriteLine("Please specify a symbol");
                    return this;
                }

                if (ChartManager.ContainsSymbol(symbol))
                {
                    Console.WriteLine("'" + symbol + "' is already known");
                    return this;
                }

                await ChartManager.InitializeSymbol(symbol);
            }

            if (text.StartsWith("Switch ", comparer))
            {
                string symbol = text.Replace("Switch ", "").Trim();
                if (string.IsNullOrEmpty(symbol))
                {
                    Console.WriteLine("Please specify a symbol");
                    return this;
                }

                if (!ChartManager.ContainsSymbol(symbol))
                {
                    Console.WriteLine("'" + symbol + "' is unknown");
                    return this;
                }

                return new SymbolMenu(ChartManager, symbol);
            }

            if (text.StartsWith("Remove ", comparer))
            {
                string symbol = text.Replace("Remove ", "").Trim();
                if (string.IsNullOrEmpty(symbol))
                {
                    Console.WriteLine("Please specify a symbol");
                    return this;
                }

                if (ChartManager.RemoveSymbol(symbol))
                {
                    Console.WriteLine("Removed '" + symbol + "'");
                }
                else
                {
                    Console.WriteLine("'" + symbol + "' is unknown");
                }

                return this;
            }

            if (string.Equals(text, "AnalyzeAll", comparer))
            {
                ChartManager.AnalyzeAll();
                return this;
            }

            if (string.Equals(text, "Rank1Year", comparer))
            {
                ChartManager.RankExponentialRegressionResultsBy1YearForecast();
                return this;
            }

            if (string.Equals(text, "Rank3Year", comparer))
            {
                ChartManager.RankExponentialRegressionResultsBy3YearForecast();
                return this;
            }

            return this;
        }
    }
}