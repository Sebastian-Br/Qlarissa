using Charty.Chart;
using Charty.Chart.ChartAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartyTests
{
    [TestClass]
    public class ExponentialRegressionTests
    {
        [TestMethod]
        [DynamicData(nameof(MockBusinessChart_20PercentYoY))]
        public void ExponentialRegressionParameterAssignment(Symbol MockBusinessChart_20PercentYoY)
        {
            ExponentialRegression exponentialRegression = new(MockBusinessChart_20PercentYoY.ChartDataPoints, 123.45);
            Assert.IsTrue(exponentialRegression.B > 1.19);
            Assert.IsTrue(exponentialRegression.B < 1.21);
        }

        public static IEnumerable<object[]> MockBusinessChart_20PercentYoY
        {
            get
            {
                SymbolDataPoint[] expectedChartDataPoints = [
                    new SymbolDataPoint(){ HighPrice = 101, LowPrice = 99, MediumPrice = 100, Date = new DateOnly ( 2020, 1, 1 ), DayIndex = 1},
                    new SymbolDataPoint(){ HighPrice = 121, LowPrice = 119, MediumPrice = 120, Date = new DateOnly ( 2021, 1, 1 ), DayIndex = 366},
                    new SymbolDataPoint(){ HighPrice = 145, LowPrice = 143, MediumPrice = 144, Date = new DateOnly ( 2022, 1, 1 ), DayIndex = 731},
                    new SymbolDataPoint(){ HighPrice = 173, LowPrice = 172, MediumPrice = 172.5, Date = new DateOnly ( 2023, 1, 1 ), DayIndex = 1096},
                    new SymbolDataPoint(){ HighPrice = 208, LowPrice = 207, MediumPrice = 207.5, Date = new DateOnly ( 2024, 1, 1 ), DayIndex = 1461},
                    ]; // The DayIndex was calculated using https://www.timeanddate.com/date/durationresult.html?d1=1&m1=1&y1=2020&d2=1&m2=1&y2=2023

                Symbol expectedChart = new(expectedChartDataPoints, new SymbolOverview());
                yield return new object[] { expectedChart };
            }
        }
    }
}