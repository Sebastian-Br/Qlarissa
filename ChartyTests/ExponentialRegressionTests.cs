using Charty.Chart;
using Charty.Chart.Analysis.ExponentialRegression;
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
            ExponentialRegression exponentialRegression = new(MockBusinessChart_20PercentYoY);
            Assert.IsTrue(exponentialRegression.B > 1.19);
            Assert.IsTrue(exponentialRegression.B < 1.21);
        }

        [TestMethod]
        [DynamicData(nameof(MockBusinessChart_ADBE))]
        public void ExponentialRegressionParameterAssignment_ADBE(Symbol MockBusinessChart_ADBE)
        {
            ExponentialRegression exponentialRegression = new(MockBusinessChart_ADBE);
            Assert.IsTrue(exponentialRegression.B > 1.19);
            Assert.IsTrue(exponentialRegression.B < 1.26);
        }

        [TestMethod]
        [DynamicData(nameof(MockBusinessChart_JKHY))]
        public void ExponentialRegressionParameterAssignment_JKHY(Symbol MockBusinessChart_JKHY)
        {
            ExponentialRegression exponentialRegression = new(MockBusinessChart_JKHY);
            Assert.IsTrue(exponentialRegression.B > 1.18);
            Assert.IsTrue(exponentialRegression.B < 1.22);
        }

        public static IEnumerable<object[]> MockBusinessChart_20PercentYoY
        {
            get
            {
                SymbolDataPoint[] expectedChartDataPoints = [
                    new SymbolDataPoint(){ HighPrice = 101, LowPrice = 99, MediumPrice = 100, Date = new DateOnly ( 2020, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 121, LowPrice = 119, MediumPrice = 120, Date = new DateOnly ( 2021, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 145, LowPrice = 143, MediumPrice = 144, Date = new DateOnly ( 2022, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 173, LowPrice = 172, MediumPrice = 172.5, Date = new DateOnly ( 2023, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 208, LowPrice = 207, MediumPrice = 207.5, Date = new DateOnly ( 2024, 1, 1 )},
                    ];

                Symbol expectedChart = new(expectedChartDataPoints, new SymbolOverview());
                yield return new object[] { expectedChart };
            }
        }

        public static IEnumerable<object[]> MockBusinessChart_ADBE
        {
            get
            {
                SymbolDataPoint[] expectedChartDataPoints = [
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 32.39, Date = new DateOnly ( 2010, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 33.25, Date = new DateOnly ( 2011, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 31.20, Date = new DateOnly ( 2012, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 38.2, Date = new DateOnly ( 2013, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 59.8, Date = new DateOnly ( 2014, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 70.44, Date = new DateOnly ( 2015, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 88.16, Date = new DateOnly ( 2016, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 113.31, Date = new DateOnly ( 2017, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 119.12, Date = new DateOnly ( 2018, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 247.82, Date = new DateOnly ( 2019, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 370.01, Date = new DateOnly ( 2023, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 619, Date = new DateOnly ( 2024, 1, 1 )},
                    ];

                Symbol expectedChart = new(expectedChartDataPoints, new SymbolOverview());
                yield return new object[] { expectedChart };
            }
        }

        public static IEnumerable<object[]> MockBusinessChart_JKHY
        {
            get
            {
                SymbolDataPoint[] expectedChartDataPoints = [
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 29.35, Date = new DateOnly ( 2011, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 34.44, Date = new DateOnly ( 2012, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 39.6, Date = new DateOnly ( 2013, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 58.85, Date = new DateOnly ( 2014, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 62.64, Date = new DateOnly ( 2015, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 76.82, Date = new DateOnly ( 2016, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 88.92, Date = new DateOnly ( 2017, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 117.32, Date = new DateOnly ( 2018, 1, 1 )},
                    new SymbolDataPoint(){ HighPrice = 0, LowPrice = 0, MediumPrice = 133.54, Date = new DateOnly ( 2019, 1, 1 )},
                    ];

                Symbol expectedChart = new(expectedChartDataPoints, new SymbolOverview());
                yield return new object[] { expectedChart };
            }
        }
    }
}