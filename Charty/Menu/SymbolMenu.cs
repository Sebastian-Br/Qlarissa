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
                "Get1YearForecast - Gets the forecast (today's date + 1 year) based on the Exponential Regression Model\n" +
                "Get3YearForecast - Gets the forecast (today's date + 3 years) based on the Exponential Regression Model\n" +
                "Set CurrentPrice x - Sets the Current Price used in the Regression Model to x\n" +
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

            if (string.Equals(text, "Get1YearForecast", comparer))
            {
                Symbol.RunRegressions_IfNotExists();
                Console.WriteLine(Symbol.ExponentialRegressionModel.GetExpectedOneYearPerformance_AsText());
                return this;
            }

            if (string.Equals(text, "Get3YearForecast", comparer))
            {
                Symbol.RunRegressions_IfNotExists();
                Console.WriteLine(Symbol.ExponentialRegressionModel.GetExpectedThreeYearPerformance_AsText());
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

            if (text.StartsWith("Set CurrentPrice ", comparer))
            {
                string priceString = text.Replace("Set CurrentPrice ", "");
                double price = double.Parse(priceString);
                Symbol.RunRegressions_IfNotExists();
                Symbol.ExponentialRegressionModel.SetTemporaryEstimates(price);
                Console.WriteLine(Symbol.ExponentialRegressionModel.GetExpectedOneYearPerformance_AsText());
                Console.WriteLine(Symbol.ExponentialRegressionModel.GetExpectedThreeYearPerformance_AsText());
                return this;
            }

            return this;
        }
    }
}