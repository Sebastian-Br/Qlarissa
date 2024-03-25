using Charty.Chart;
using Charty.Chart.Api.ApiChart;

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

            Symbol chart = apiChart.ToBusinessChart(apiOverview);

            Symbol expectedChart = ApiChart_ExpectedChartTuple.Item2;

            for(int i = 0; i < chart.ChartDataPoints.Length; i++)
            {
                Assert.AreEqual(expectedChart.ChartDataPoints[i].Date, chart.ChartDataPoints[i].Date);
                Assert.AreEqual(expectedChart.ChartDataPoints[i].HighPrice, chart.ChartDataPoints[i].HighPrice);
                Assert.AreEqual(expectedChart.ChartDataPoints[i].LowPrice, chart.ChartDataPoints[i].LowPrice);
                Assert.AreEqual(expectedChart.ChartDataPoints[i].MediumPrice, chart.ChartDataPoints[i].MediumPrice);
                Assert.AreEqual(expectedChart.ChartDataPoints[i].DayIndex, chart.ChartDataPoints[i].DayIndex);
            }

            Assert.AreEqual(expectedChart.SymbolOverview.Name, chart.SymbolOverview.Name);
            Assert.AreEqual(expectedChart.SymbolOverview.Symbol, chart.SymbolOverview.Symbol);
            Assert.AreEqual(expectedChart.SymbolOverview.PEratio, chart.SymbolOverview.PEratio);
            Assert.AreEqual(expectedChart.SymbolOverview.MarketCapitalization, chart.SymbolOverview.MarketCapitalization);
            Assert.AreEqual(expectedChart.SymbolOverview.Currency, chart.SymbolOverview.Currency);
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

                ChartDataPoint[] expectedChartDataPoints = [
                    new ChartDataPoint(){ HighPrice = 101, LowPrice = 99, MediumPrice = 100, Date = new DateOnly ( 2020, 1, 1 ), DayIndex = 1},
                    new ChartDataPoint(){ HighPrice = 121, LowPrice = 119, MediumPrice = 120, Date = new DateOnly ( 2021, 1, 1 ), DayIndex = 366},
                    new ChartDataPoint(){ HighPrice = 145, LowPrice = 143, MediumPrice = 144, Date = new DateOnly ( 2022, 1, 1 ), DayIndex = 731},
                    new ChartDataPoint(){ HighPrice = 173, LowPrice = 172, MediumPrice = 172.5, Date = new DateOnly ( 2023, 1, 1 ), DayIndex = 1096},
                    ]; // The DayIndex was calculated using https://www.timeanddate.com/date/durationresult.html?d1=1&m1=1&y1=2020&d2=1&m2=1&y2=2023

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