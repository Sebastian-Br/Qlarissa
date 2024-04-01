using Charty.Chart.Analysis.ExponentialRegression;
using Charty.Chart.Enums;
using MathNet.Numerics;
using ScottPlot;

namespace Charty.Chart.Analysis.CascadingCAGR
{
    public class ProjectingCAGR : IRegressionResult
    {
        // y = y0 * g(t) ^ (t - x0)
        // y0 and x0 are constants. B is derived from the original regression model.
        public ProjectingCAGR(Symbol symbol)
        {
            Symbol = symbol;
            BaseRegression = symbol.ExponentialRegressionModel;
            y0 = symbol.DataPoints[0].MediumPrice;
            x0 = symbol.DataPoints[0].Date.ToDouble();
            //Console.WriteLine("CascadingCAGR: " + y0 + " * " + BaseRegression.B + " ^ (t-" + x0 + ")");
            //Console.WriteLine("CascadingCAGR: " + symbol);

            // first cascading exp-Regression will start at datapoints[0].Date and end at the first dataPoint with date >= that date.AddMonths(earliestAnalasysEndDate_Delta_Months)
            int earliestAnalasysEndDate_Delta_Months = 48;
            SymbolDataPoint firstDataPoint = symbol.DataPoints[0];
            DateOnly firstDataPointDate = firstDataPoint.Date;
            DateOnly firstTargetDate = symbol.DataPoints.First(x => x.Date >= firstDataPointDate.AddMonths(earliestAnalasysEndDate_Delta_Months)).Date;

            DateOnly targetDate = firstTargetDate;
            Dictionary<DateOnly, double> CAGRs_UntilDate = new();
            double initialA = 1.0;
            double initialB = 1.0;

            while (targetDate <= symbol.DataPoints.Last().Date)
            {
                //Console.WriteLine("Target Date: " + targetDate);
                SymbolDataPoint[] dataPoints_untilTargetDate = symbol.DataPoints.Where(x => x.Date <= targetDate).ToArray();
                SymbolDataPoint lastDataPoint_untilTargetDate = dataPoints_untilTargetDate.Last();
                //ExponentialRegression.ExponentialRegression e = new(dataPoints_untilTargetDate, initialA, initialB);
                double cagr = Math.Pow((BaseRegression.GetEstimate(lastDataPoint_untilTargetDate.Date)) /(firstDataPoint.MediumPrice), (1.0/(lastDataPoint_untilTargetDate.Date.ToDouble() - firstDataPointDate.ToDouble())));
                CAGRs_UntilDate.Add(targetDate, cagr);
                //initialA = e.A; initialB = e.B;
                targetDate = targetDate.AddDays(7);
            }

            GrowthRateRegressions = new();
            GrowthRateRegressions.Add(GetLinearRegression(CAGRs_UntilDate));
            GrowthRateRegressions.Add(GetLogisticRegression(CAGRs_UntilDate));
            GrowthRateRegressions.Sort((a, b) => b.GetRsquared().CompareTo(a.GetRsquared()));
            CalculateRsquared();
            DateCreated = DateOnly.FromDateTime(DateTime.Now);
            RegressionResult = RegressionResultType.ProjectingCAGR;
            //PlotDictionary_WithBestRegression(CAGRs_UntilDate);
            //PlotDictionary(RegularExpressionBs_UntilDate);
        }

        public ExponentialRegression.ExponentialRegressionResult BaseRegression { get; private set; }

        public double x0 { get; private set; }
        public double y0 { get; private set; }

        public double Rsquared { get; private set; }

        private Symbol Symbol { get; set; }

        internal List<IRegressionResult> GrowthRateRegressions { get; private set; }

        DateOnly DateCreated { get; set; }

        RegressionResultType RegressionResult { get; set; }

        private void CalculateRsquared()
        {
            SymbolDataPoint[] dataPoints;
            if (GrowthRateRegressions[0] is LogisticRegressionResult result)
            {
                dataPoints = Symbol.DataPoints.Where(x => x.Date.ToDouble() > result.ConstantT).ToArray();
            }
            else
            {
                dataPoints = Symbol.DataPoints;
            }

            double[] Xs = dataPoints.Select(x => x.Date.ToDouble()).ToArray();
            double[] Ys = dataPoints.Select(x => x.MediumPrice).ToArray();

            Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => GetEstimate(x)), Ys);
        }

        public double GetEstimate(double t)
        {
            double adjustedB = GrowthRateRegressions[0].GetEstimate(t);
            return y0 * Math.Pow(adjustedB, (t - x0));
        }

        public double GetEstimate(DateOnly date)
        {
            double t = date.ToDouble();
            return GetEstimate(t);
        }

        private void PlotDictionary_WithBestRegression(Dictionary<DateOnly, double> cagrResults)
        {
            double[] x = cagrResults.Select(kvp => kvp.Key.ToDouble()).ToArray();
            double[] y = cagrResults.Select(kvp => kvp.Value).ToArray();

            ScottPlot.Plot myPlot = new();
            var Scatter = myPlot.Add.Scatter(x, y);

            ScottPlot.Palettes.Category20 palette = new();
            Scatter.Color = palette.Colors[2];
            //myPlot.FigureBackground.Color = Color.FromHex("#0b3049");
            myPlot.Title("PCAGR Growth Rate with Regression:\n" + GrowthRateRegressions.First().ToString());
            //myPlot.Axes.SetLimitsY(AxisLimits.VerticalOnly(0.9, 1.6));
            myPlot.Axes.SetLimitsX(AxisLimits.HorizontalOnly(2008, 2026));

            IRegressionResult bestRegression = GrowthRateRegressions.First();

            List<double> Xs = new();
            List<double> bestYs = new();

            foreach(DateOnly key in  cagrResults.Keys)
            {
                double loopT = key.ToDouble();
                //Xs.Add(loopT);
                //bestYs.Add(bestRegression.GetEstimate(key));
            }

            for(double d = 2008; d < 2026; d += 0.01)
            {
                Xs.Add(d);
                bestYs.Add(bestRegression.GetEstimate(d));
            }

            var scatter = myPlot.Add.Scatter(Xs, bestYs);

            scatter.LineWidth = 1.0f;
            if(bestRegression.GetRegressionResultType() == Enums.RegressionResultType.Linear)
            {
                scatter.Color = Colors.Red;
            }
            else
            {
                scatter.Color = Colors.DarkGreen;
            }

            myPlot.SavePng(BaseRegression.Overview.Symbol + "_PCAGR36_WithRegression.png", 1000, 500);
        }

        /// <summary>
        /// https://numerics.mathdotnet.com/Regression#Curve-Fitting-Linear-Regression
        /// </summary>
        /// <param name="cagrResults"></param>
        /// <returns></returns>
        private LinearRegressionResult GetLinearRegression(Dictionary<DateOnly, double> cagrResults)
        {
            // using x0 provides no benefits here
            double[] x = cagrResults.Select(kvp => kvp.Key.ToDouble()).ToArray();
            double[] y = cagrResults.Select(kvp => kvp.Value).ToArray();
            var p = Fit.Line(x, y);

            double c = p.Item1; // intercept
            double m = p.Item2; // slope
            double rSquared = GoodnessOfFit.RSquared(x.Select(x => c + m * x), y);
            LinearRegressionResult result = new(rSquared, m, c);
            return result;
        }

        /// <summary>
        /// https://numerics.mathdotnet.com/Regression#Curve-Fitting-Linear-Regression
        /// </summary>
        /// <param name="cagrResults"></param>
        /// <returns></returns>
        private LogisticRegressionResult GetLogisticRegression(Dictionary<DateOnly, double> cagrResults)
        {
            double min_xDelta0 = -0.001;
            double xDelta0 = min_xDelta0;
            double firstDateIndex = cagrResults.Keys.First().ToDouble();
            double currentX0 = firstDateIndex - xDelta0;

            int maxIterations = 100000000;
            int iteration = 0;

            double currentBest_rSquared = 0;
            double stepSize = -0.0001;

            double a = 0, b = 0, rSquared = 0;

            while( iteration < maxIterations ) // optimize with exponential-walk gobbledigook.
            {
                currentX0 = firstDateIndex + xDelta0;

                double[] x = cagrResults.Select(kvp => (kvp.Key.ToDouble() - currentX0)).ToArray();
                double[] y = cagrResults.Select(kvp => kvp.Value).ToArray();
                var p = Fit.Logarithm(x, y);
                // a + b * ln(x)
                a = p.Item1;
                b = p.Item2;
                rSquared = GoodnessOfFit.RSquared(x.Select(x => a + b * Math.Log(x)), y);

                if (rSquared > currentBest_rSquared)
                {
                    currentBest_rSquared = rSquared;
                }
                else
                {
                    break;
                }

                xDelta0 = xDelta0 + stepSize;
                iteration++;
            }

            LogisticRegressionResult result = new(rSquared, A: b, B: a, _x0: currentX0, cagrResults.Keys.First().ToDouble()); // different definition in the class
            return result;
        }

        private void PlotDictionary(Dictionary<DateOnly, double> cagrResults)
        {
            double[] x = cagrResults.Select(kvp => kvp.Key.ToDouble()).ToArray();
            double[] y = cagrResults.Select(kvp => kvp.Value).ToArray();
            //int numberOfDataPoints = mediumPrices.Length;

            double firstYearIndex = cagrResults.Keys.First().ToDouble();
            double lastYearIndex = cagrResults.Keys.Last().ToDouble();

            ScottPlot.Plot myPlot = new();
            var Scatter = myPlot.Add.Scatter(x, y);

            ScottPlot.Palettes.Category20 palette = new();
            Scatter.Color = palette.Colors[2];
            myPlot.FigureBackground.Color = Color.FromHex("#0b3049");
            myPlot.Title("PCAGR");
            //myPlot.Axes.SetLimitsY(AxisLimits.VerticalOnly(0.0, 1.6));
            myPlot.SavePng("PCAGR_INITIALIZED_NOLIMIT36.png", 600, 400);
        }

        public List<double> GetParameters()
        {
            throw new NotImplementedException();
        }

        public double GetRsquared()
        {
            return Rsquared;
        }

        public DateOnly GetCreationDate()
        {
            return DateCreated;
        }

        public RegressionResultType GetRegressionResultType()
        {
            return RegressionResult;
        }
    }
}