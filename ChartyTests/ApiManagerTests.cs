using Charty.Chart;
using Charty.Chart.Api.AlphaVantage.ApiSymbol;

namespace ChartyTests
{
    [TestClass]
    public class ApiManagerTests
    {
        [TestMethod]
        [DynamicData(nameof(MockApiChart_AndExpectedBusinessChart_20PercentYoY))]
        public void ApiChartToBusinessChart(Tuple<ApiSymbol, Symbol> ApiChart_ExpectedChartTuple)
        {
            ApiSymbol apiChart = ApiChart_ExpectedChartTuple.Item1;
            ApiOverview apiOverview = new();
            apiOverview.Symbol = "TEST";
            apiOverview.PERatio = "31.415";
            apiOverview.Name = "TEST Corporation";
            apiOverview.MarketCapitalization = "123456789012";
            apiOverview.Currency = "USD";

            Symbol chart = apiChart.ToBusinessChart(apiOverview.ToBusinessOverview());

            Symbol expectedChart = ApiChart_ExpectedChartTuple.Item2;

            for(int i = 0; i < chart.DataPoints.Length; i++)
            {
                Assert.AreEqual(expectedChart.DataPoints[i].Date, chart.DataPoints[i].Date);
                Assert.AreEqual(expectedChart.DataPoints[i].HighPrice, chart.DataPoints[i].HighPrice);
                Assert.AreEqual(expectedChart.DataPoints[i].LowPrice, chart.DataPoints[i].LowPrice);
                Assert.AreEqual(expectedChart.DataPoints[i].MediumPrice, chart.DataPoints[i].MediumPrice);
            }

            Assert.AreEqual(expectedChart.Overview.Name, chart.Overview.Name);
            Assert.AreEqual(expectedChart.Overview.Symbol, chart.Overview.Symbol);
            Assert.AreEqual(expectedChart.Overview.PEratio, chart.Overview.PEratio);
            Assert.AreEqual(expectedChart.Overview.MarketCapitalization, chart.Overview.MarketCapitalization);
            Assert.AreEqual(expectedChart.Overview.Currency, chart.Overview.Currency);
        }

        public static IEnumerable<object[]> MockApiChart_AndExpectedBusinessChart_20PercentYoY
        {
            get
            {
                ApiSymbol apiChart = new();
                Dictionary<DateOnly, ApiChartDataPoint> apiChartDataPoints = new()
                {
                    { new DateOnly ( 2020, 1, 1 ), new ApiChartDataPoint() {high = "101", low = "99"} }, // Format: Year, Month, Day
                    { new DateOnly ( 2021, 1, 1 ), new ApiChartDataPoint() {high = "121", low = "119"} },
                    { new DateOnly ( 2022, 1, 1 ), new ApiChartDataPoint() {high = "145", low = "143"} },
                    { new DateOnly ( 2023, 1, 1 ), new ApiChartDataPoint() {high = "173", low = "172"} },
                };
                apiChart.DataPoints = apiChartDataPoints;

                SymbolDataPoint[] expectedChartDataPoints = [
                    new SymbolDataPoint(){ HighPrice = 101, LowPrice = 99, MediumPrice = 100, Date = new DateOnly ( 2020, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 121, LowPrice = 119, MediumPrice = 120, Date = new DateOnly ( 2021, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 145, LowPrice = 143, MediumPrice = 144, Date = new DateOnly ( 2022, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 173, LowPrice = 172, MediumPrice = 172.5, Date = new DateOnly ( 2023, 1, 1 )},
                    ];

                SymbolOverview overview = new SymbolOverview();
                overview.Currency = Charty.Chart.Enums.Currency.USD_US_DOLLAR;
                overview.MarketCapitalization = 123456789012;
                overview.PEratio = 31.415;
                overview.Symbol = "TEST";
                overview.Name = "TEST Corporation";
                Symbol expectedChart = new(expectedChartDataPoints, overview);
                yield return new object[] { new Tuple<ApiSymbol, Symbol>(apiChart, expectedChart) };
            }
        }
    }
}