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
        public SymbolManager SymbolManager { get; set; }

        public StartMenu(SymbolManager symbolManager)
        {
            SymbolManager = symbolManager;
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
                "Add SYMBOL - Adds the Symbol to the Chart Dictionary (and DB)\n" +
                "Add ConfigSymbols - Adds all Symbols in the customConfiguration\n" +
                "Switch SYMBOL - Switch to the Symbol's Menu\n" +
                "Remove SYMBOL - Removes the Symbol from the Chart Dictionary\n" +
                "AnalyzeAll - Run analyses (Exponential Regression) on all Symbols\n" +
                "Rank1Year - Ranks all Symbols according to their expected 1-year performance\n" +
                "Rank3Year - Ranks all Symbols according to their expected 3-year performance\n" +
                "RankAggregate - Ranks all Symbols according an aggregated score\n" +
                "Draw SYMBOL - Draws a symbol's chart\n" +
                "DrawAll - Draws all symbols' charts\n"
                ;
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

                if (SymbolManager.ContainsSymbol(symbol))
                {
                    Console.WriteLine("'" + symbol + "' is already known");
                    return this;
                }

                if(string.Equals(symbol, "ConfigSymbols", comparer))
                {
                    await SymbolManager.AddConfigurationSymbols();
                    return this;
                }

                await SymbolManager.InitializeSymbolFromAPI(symbol);
                return this;
            }

            if (text.StartsWith("Switch ", comparer))
            {
                string symbol = text.Replace("Switch ", "").Trim();
                if (string.IsNullOrEmpty(symbol))
                {
                    Console.WriteLine("Please specify a symbol");
                    return this;
                }

                if (!SymbolManager.ContainsSymbol(symbol))
                {
                    Console.WriteLine("'" + symbol + "' is unknown");
                    return this;
                }

                return new SymbolMenu(SymbolManager, symbol);
            }

            if (text.StartsWith("Remove ", comparer))
            {
                string symbol = text.Replace("Remove ", "").Trim();
                if (string.IsNullOrEmpty(symbol))
                {
                    Console.WriteLine("Please specify a symbol");
                    return this;
                }

                if (SymbolManager.RemoveSymbol(symbol))
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
                SymbolManager.AnalyzeAll();
                return this;
            }

            if (string.Equals(text, "DrawAll", comparer))
            {
                SymbolManager.DrawAll();
                return this;
            }

            if (string.Equals(text, "Rank1Year", comparer))
            {
                Console.WriteLine(SymbolManager.RankBy1YearForecast());
                return this;
            }

            if (string.Equals(text, "Rank3Year", comparer))
            {
                Console.WriteLine(SymbolManager.RankBy3YearForecast());
                return this;
            }

            if (string.Equals(text, "RankAggregate", comparer))
            {
                Console.WriteLine(SymbolManager.RankByAggregateScore());
                return this;
            }

            if (text.StartsWith("Draw ", comparer))
            {
                string symbol = text.Replace("Draw ", "").Trim();
                if (string.IsNullOrEmpty(symbol))
                {
                    Console.WriteLine("Please specify a symbol");
                    return this;
                }

                if (SymbolManager.ContainsSymbol(symbol))
                {
                    SymbolManager.Draw(symbol);
                    Console.WriteLine("Drew " + symbol);
                }

                return this;
            }

            return this;
        }
    }
}