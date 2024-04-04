using Charty.Chart;
using Charty.CustomConfiguration;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartyTests
{
    [TestClass]
    internal class DataBaseTests
    {

        // add tests



        static void TestDBsave(IConfiguration baseConfiguration, CustomConfiguration customConfiguration)
        {

            Charty.Database.DB db = new(baseConfiguration);
            SymbolDataPoint[] points = [ new SymbolDataPoint() { Date = new DateOnly(2023,1,1), HighPrice = 50, LowPrice = 40, MediumPrice = 45},
            new SymbolDataPoint() { Date = new DateOnly(2023,1,2), HighPrice = 51, LowPrice = 41, MediumPrice = 46},
            new SymbolDataPoint() { Date = new DateOnly(2023,1,3), HighPrice = 52, LowPrice = 42, MediumPrice = 47},
            ];
            SymbolOverview overview = new SymbolOverview() { Currency = Charty.Chart.Enums.Currency.USD, DividendPerShareYearly = 1.2, MarketCapitalization = 0, Name = "TEST Inc.", Symbol = "TST", TrailingPE = 0 };
            Symbol symbol = new(points, overview);
            symbol.RunRegressions_IfNotExists();
            db.InsertOrUpdateSymbolInformation(symbol);
        }

        static void TestDBsave2(IConfiguration baseConfiguration, CustomConfiguration customConfiguration)
        {

            Charty.Database.DB db = new(baseConfiguration);
            SymbolDataPoint[] points = [ new SymbolDataPoint() { Date = new DateOnly(2023,1,1), HighPrice = 50, LowPrice = 40, MediumPrice = 45},
            new SymbolDataPoint() { Date = new DateOnly(2023,1,2), HighPrice = 51, LowPrice = 41, MediumPrice = 46},
            new SymbolDataPoint() { Date = new DateOnly(2023,1,3), HighPrice = 52, LowPrice = 42, MediumPrice = 47},
            new SymbolDataPoint() { Date = new DateOnly(2023,1,4), HighPrice = 53, LowPrice = 43, MediumPrice = 48},
            ];
            SymbolOverview overview = new SymbolOverview() { Currency = Charty.Chart.Enums.Currency.USD, DividendPerShareYearly = 1.37, MarketCapitalization = 0, Name = "TEST Inc.", Symbol = "TST", TrailingPE = 0 };
            Symbol symbol = new(points, overview);
            symbol.RunRegressions_IfNotExists();
            db.InsertOrUpdateSymbolInformation(symbol);
        }
    }
}