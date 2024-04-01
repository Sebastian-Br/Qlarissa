using Charty.Chart;

namespace Charty.Menu
{
    public class SymbolMenu : IMenu
    {
        public SymbolMenu(SymbolManager chartManager, string symbol)
        {
            SymbolManager = chartManager;
            Symbol = SymbolManager.RetrieveSymbol(symbol);
            PrintNameAndMenu();
        }
        public SymbolManager SymbolManager { get; set; }

        private Chart.Symbol Symbol { get; set; }

        public string HelpMenu()
        {
            return "R - Reload Help Menu\n" +
                "Analyze - Run analyses (ExponentialRegression)\n" +
                //"Get1YearForecast - Gets the forecast (today's date + 1 year) based on the Exponential Regression Model\n" +
                //"Get3YearForecast - Gets the forecast (today's date + 3 years) based on the Exponential Regression Model\n" +
                "DbgPrintDataPoints - Prints the Data Points for Debugging Purposes\n" +
                "Exit - Return to the Main Menu\n";
        }

        public string StateName()
        {
            return "Symbol Menu.\n";
        }

        public void PrintNameAndMenu()
        {
            Console.WriteLine(StateName());
            Console.WriteLine(HelpMenu());
            Console.WriteLine(Symbol.ToString());
        }

        public async Task<IMenu> SendText(string text)
        {
            StringComparison comparer = StringComparison.InvariantCultureIgnoreCase;
            if (string.IsNullOrEmpty(text))
            {
                return this;
            }

            if (string.Equals(text, "R"))
            {
                Console.WriteLine(HelpMenu());
                return this;
            }

            if(string.Equals(text, "Analyze", comparer))
            {
                Symbol.RunRegressions_IfNotExists();
                return this;
            }

            if (string.Equals(text, "Exit", comparer))
            {
                return new StartMenu(SymbolManager);
            }

            if (string.Equals(text, "DbgPrintDataPoints", comparer))
            {
                Symbol.DbgPrintDataPoints();
                return this;
            }

            return this;
        }
    }
}