using Qlarissa.Chart.Analysis.BaseRegressions;
using Qlarissa.Chart.Analysis.ExponentialRegression;
using Qlarissa.Chart.Enums;
using Qlarissa.CustomConfiguration;
using MathNet.Numerics;
using ScottPlot;

namespace Qlarissa.Chart.Analysis.InverseLogRegression;

public class InverseLogRegressionResult : IRegressionResult
{
    /// <summary>
    /// y(t) = e^(g(t)), where g is the best regression (log, linear, exp) for log(data)
    /// </summary>
    /// <param name="symbol"></param>
    public InverseLogRegressionResult(Symbol symbol)
    {
        SymbolDataPoint[] dataPoints = symbol.GetDataPointsForAnalysis();
        PreprocessingX0 = - 2000.0;
        double[] Xs = dataPoints.Select(dataPoint => dataPoint.Date.ToDouble()).ToArray();

        double[] preProcessedXs = dataPoints.Select(dataPoint => dataPoint.Date.ToDouble() + PreprocessingX0).ToArray();
        double[] logYs = dataPoints.Select(dataPoint => Math.Log(dataPoint.MediumPrice)).ToArray();

        InnerRegressions = new();
        LogisticRegressionResult logisticRegressionResult = GetLogisticRegression_ExpWalk(preProcessedXs, logYs);
        InnerRegressions.Add(logisticRegressionResult);

        LinearRegressionResultWithX0 linearRegression = new(preProcessedXs, logYs, PreprocessingX0);
        InnerRegressions.Add(linearRegression);

        ExponentialRegression.ExponentialRegression expReg = new(Xs, logYs, -PreprocessingX0); // does preprocessing internally
        ExponentialRegression.ExponentialRegressionResult exponentialRegression = new(expReg, Xs, logYs);
        InnerRegressions.Add(exponentialRegression);

        LogCappedRegressionResult logCappedReg = new(Xs, logYs); // does preprocessing internally!
        InnerRegressions.Add(logCappedReg);

        InnerRegressions.Sort((a, b) => b.GetRsquared().CompareTo(a.GetRsquared())); // sorts regressions in descending order with respect to R²

        DrawWithLogReg(Xs, logYs, symbol);

        double[] Ys = dataPoints.Select(dataPoint => dataPoint.MediumPrice).ToArray();
        Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => GetEstimate(x)), Ys);
        DateCreated = DateOnly.FromDateTime(DateTime.Now);

        TrainingPeriodDays = GetExactDaysDifference(dataPoints[0].Date, dataPoints.Last().Date);
        SlopeAtEndOfTrainingPeriod = GetSlopeAtDate(dataPoints.Last().Date);
    }

    /// <summary>
    /// Used when analyzing a subset of datapoints
    /// </summary>
    /// <param name="dataPoints"></param>
    public InverseLogRegressionResult(SymbolDataPoint[] dataPoints)
    {
        PreprocessingX0 = -2000.0;
        double[] Xs = dataPoints.Select(dataPoint => dataPoint.Date.ToDouble()).ToArray();

        double[] preProcessedXs = dataPoints.Select(dataPoint => dataPoint.Date.ToDouble() + PreprocessingX0).ToArray();
        double[] logYs = dataPoints.Select(dataPoint => Math.Log(dataPoint.MediumPrice)).ToArray();

        InnerRegressions = new();
        LogisticRegressionResult logisticRegressionResult = GetLogisticRegression_ExpWalk(preProcessedXs, logYs);
        InnerRegressions.Add(logisticRegressionResult);

        LinearRegressionResultWithX0 linearRegression = new(preProcessedXs, logYs, PreprocessingX0);
        InnerRegressions.Add(linearRegression);

        ExponentialRegression.ExponentialRegression expReg = new ExponentialRegression.ExponentialRegression(Xs, logYs, -PreprocessingX0); // does preprocessing internally
        ExponentialRegression.ExponentialRegressionResult exponentialRegression = new(expReg, Xs, logYs);
        InnerRegressions.Add(exponentialRegression);

        InnerRegressions.Sort((a, b) => b.GetRsquared().CompareTo(a.GetRsquared())); // sorts regressions in descending order with respect to R²

        //DrawWithLogReg(Xs, logYs, symbol);

        double[] Ys = dataPoints.Select(dataPoint => dataPoint.MediumPrice).ToArray();
        Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => GetEstimate(x)), Ys);
        DateCreated = DateOnly.FromDateTime(DateTime.Now);
    }

    DateOnly DateCreated {  get; set; }

    //List<double> Parameters { get; set; }

    double Rsquared { get; set; }

    double PreprocessingX0 { get; set; }

    RegressionResultType RegressionResult { get; set; } = RegressionResultType.InverseLogistic;

    List<IRegressionResult> InnerRegressions { get; set; }

    public DateOnly GetCreationDate()
    {
        return DateCreated;
    }

    public double GetEstimate(DateOnly date)
    {
        return GetEstimate(date.ToDouble());
    }

    public double GetEstimate(double t)
    {
        IRegressionResult bestRegression = InnerRegressions[0];
        if(bestRegression is LogisticRegressionResult)
        {
            t += PreprocessingX0;
        }

        return Math.Exp(bestRegression.GetEstimate(t));
    }

    public List<double> GetParameters()
    {
        return null;
    }

    public RegressionResultType GetRegressionResultType()
    {
        return RegressionResult;
    }

    public double GetRsquared()
    {
        return Rsquared;
    }

    public override string ToString()
    {
        throw new NotImplementedException();
    }

    public double GetWeight()
    {
        double weight = 1.0 / (1.0 - GetRsquared());
        return weight * weight;
    }

    private void DrawWithLogReg(double[] Xs, double[] Ys, Symbol symbol)
    {
        ScottPlot.Plot myPlot = new();
        Ys = Ys.Select(y => y).ToArray(); // let's pretend this line does not exist.
        var symbolScatter = myPlot.Add.Scatter(Xs, Ys);
        ScottPlot.Palettes.Category20 palette = new();
        symbolScatter.Color = palette.Colors[2];
        symbolScatter.LineWidth = 0.5f;

        myPlot.Title("Log of " + symbol.Overview.Symbol + " with Regressions");

        List<double> listXs = new();
        List<double> listLogRegYs = new();
        List<double> listLinRegYs = new();
        List<double> listExpRegYs = new();
        List<double> listLogCappedRegYs = new();

        LogisticRegressionResult logisticRegression = (LogisticRegressionResult)InnerRegressions.Find(regression => regression.GetRegressionResultType() == RegressionResultType.Logistic);
        LinearRegressionResultWithX0 linearRegression = (LinearRegressionResultWithX0)InnerRegressions.Find(regression => regression.GetRegressionResultType() == RegressionResultType.Linear);
        ExponentialRegressionResult exponentialRegression = (ExponentialRegressionResult)InnerRegressions.Find(regression => regression.GetRegressionResultType() == RegressionResultType.Exponential);
        LogCappedRegressionResult logCappedRegression = (LogCappedRegressionResult)InnerRegressions.Find(regression => regression.GetRegressionResultType() == RegressionResultType.LogisticallyCapped);

        for (double d = Xs.First(); d <= Xs.Last(); d += 0.01)
        {
            listXs.Add(d);
            listLogRegYs.Add(logisticRegression.GetEstimate(d + PreprocessingX0)); // logistic regression doesn't store the pre-processing x0
            listLinRegYs.Add(linearRegression.GetEstimate(d));
            listExpRegYs.Add(exponentialRegression.GetEstimate(d));
            listLogCappedRegYs.Add(logCappedRegression.GetEstimate(d));
        }

        double[] graphXs = [.. listXs];
        double[] LogRegYs = [.. listLogRegYs];
        var logScatter = myPlot.Add.Scatter(graphXs, LogRegYs);
        logScatter.Color = Colors.Green;
        logScatter.MarkerSize = 1.0f;
        logScatter.LegendText = "Logistic Regression";

        double[] LinRegYs = [.. listLinRegYs];
        var linScatter = myPlot.Add.Scatter(graphXs, LinRegYs);
        linScatter.Color = Colors.Blue;
        linScatter.MarkerSize = 1.0f;
        linScatter.LegendText = "Linear Regression";

        double[] ExpRegYs = [.. listExpRegYs];
        var expScatter = myPlot.Add.Scatter(graphXs, ExpRegYs);
        expScatter.Color = Colors.Red;
        expScatter.MarkerSize = 1.0f;
        expScatter.LegendText = "Exponential Regression";

        double[] logCappedYs = [.. listLogCappedRegYs];
        var logCappedScatter = myPlot.Add.Scatter(graphXs, logCappedYs);
        logCappedScatter.Color = Colors.DarkMagenta;
        logCappedScatter.MarkerSize = 1.0f;
        logCappedScatter.LegendText = "Logistically Capped Regression";

        //https://scottplot.net/cookbook/5.0/Annotation/AnnotationCustomize/
        var logRegAnnotation = myPlot.Add.Annotation("LogRegR²=" + logisticRegression.GetRsquared());
        logRegAnnotation.LabelFontSize = 18;
        logRegAnnotation.LabelBackgroundColor = Colors.Gray.WithAlpha(.3);
        logRegAnnotation.LabelFontColor = Colors.Black.WithAlpha(0.8);
        logRegAnnotation.LabelBorderColor = Colors.Gray.WithAlpha(0.5);
        logRegAnnotation.LabelBorderWidth = 1;

        var linRegAnnotation = myPlot.Add.Annotation("LinRegR²=" + linearRegression.GetRsquared());
        linRegAnnotation.LabelFontSize = 18;
        linRegAnnotation.LabelBackgroundColor = Colors.Gray.WithAlpha(.3);
        linRegAnnotation.LabelFontColor = Colors.Black.WithAlpha(0.8);
        linRegAnnotation.LabelBorderColor = Colors.Gray.WithAlpha(0.5);
        linRegAnnotation.LabelBorderWidth = 1;
        linRegAnnotation.OffsetY = 35;

        var expRegAnnotation = myPlot.Add.Annotation("ExpRegR²=" + exponentialRegression.GetRsquared());
        expRegAnnotation.LabelFontSize = 18;
        expRegAnnotation.LabelBackgroundColor = Colors.Gray.WithAlpha(.3);
        expRegAnnotation.LabelFontColor = Colors.Black.WithAlpha(0.8);
        expRegAnnotation.LabelBorderColor = Colors.Gray.WithAlpha(0.5);
        expRegAnnotation.LabelBorderWidth = 1;
        expRegAnnotation.OffsetY = 70;

        var logCappedRegAnnotation = myPlot.Add.Annotation("LogCappedRegR²=" + logCappedRegression.GetRsquared());
        logCappedRegAnnotation.LabelFontSize = 18;
        logCappedRegAnnotation.LabelBackgroundColor = Colors.Gray.WithAlpha(.3);
        logCappedRegAnnotation.LabelFontColor = Colors.Black.WithAlpha(0.8);
        logCappedRegAnnotation.LabelBorderColor = Colors.Gray.WithAlpha(0.5);
        logCappedRegAnnotation.LabelBorderWidth = 1;
        logCappedRegAnnotation.OffsetY = 105;


        myPlot.ShowLegend();
        int width = 630;
        double aspectRatio_HeightOverWidth = 1100.0 / 600.0;
        myPlot.SavePng(SaveLocationsConfiguration.GetLogRegressionsSaveFileLocation(symbol), (int)(aspectRatio_HeightOverWidth * width), width);
    }

    private LogisticRegressionResult GetLogisticRegression_ExpWalk(double[] Xs, double[] Ys)
    {
        double min_xDelta0 = -0.001;
        double xDelta0 = min_xDelta0;
        double firstDateIndex = Xs[0];
        double currentX0 = firstDateIndex - xDelta0;

        int maxIterations = 200000;
        int iteration = 0;

        double currentBest_rSquared = 0;
        double stepSize = -0.001;
        double exitStepSize = 1e-308; // Exit condition based on step size
        double lastValid_xDelta0 = xDelta0;
        double bestA = 1.0, bestB = 0, bestRSquared = 0;

        while (iteration < maxIterations && Math.Abs(stepSize) >= exitStepSize)
        {
            currentX0 = firstDateIndex + xDelta0;

            double[] x = new double[Xs.Length]; //TODO: No need to allocate new memory every time here
            for (int i = 0; i < Xs.Length; i++)
            {
                x[i] = Xs[i] - currentX0;
            }

            var p = Fit.Logarithm(x, Ys);
            double a = p.A;
            double b = p.B;
            double rSquared = GoodnessOfFit.RSquared(x.Select(x => a + b * Math.Log(x)), Ys);

            if (rSquared > currentBest_rSquared)
            {
                // Update current best result
                currentBest_rSquared = rSquared;
                bestA = a;
                bestB = b;
                bestRSquared = rSquared;

                // Adjust step size for the next iteration exponentially
                stepSize *= 1.5;
                lastValid_xDelta0 = xDelta0;
            }
            else
            {
                // Overshoot occurred, restore previous best result and decrease step size
                xDelta0 = lastValid_xDelta0;
                stepSize *= 0.5;

                // Check if step size is sufficiently small & exit loop immediately if so
                if (Math.Abs(stepSize) < exitStepSize)
                    break;
            }

            xDelta0 += stepSize;
            iteration++;
        }

        //Console.WriteLine("Finished Logistic Regression Exp Walk. Iterations: " + iteration);
        LogisticRegressionResult result = new(bestRSquared, A: bestB, B: bestA, _x0: firstDateIndex + lastValid_xDelta0, Xs[0]);
        return result;
    }

    public IRegressionResult GetEffectiveInnerRegression()
    {
        return InnerRegressions[0];
    }

    int TrainingPeriodDays { get; set; }

    double SlopeAtEndOfTrainingPeriod { get; set; }

    double GetSlopeAtDate(DateOnly date)
    {
        double epsilon = 1e-3;
        double slope = 0;
        double dateAsDouble = date.ToDouble();

        double dateMinusEpsilon = dateAsDouble - epsilon;
        double datePlusEpsilon = dateAsDouble + epsilon;
        double dx = 2 * epsilon;
        double dy = GetEstimate(datePlusEpsilon) - GetEstimate(dateMinusEpsilon);
        slope = dy / dx;

        return slope;
    }

    static int GetExactDaysDifference(DateOnly startDate, DateOnly endDate)
    {
        int daysDifference = 0;

        int startYear = startDate.Year;
        int endYear = endDate.Year;

        for (int year = startYear; year < endYear; year++)
        {
            daysDifference += DateTime.IsLeapYear(year) ? 366 : 365;
        }

        daysDifference += endDate.DayOfYear - startDate.DayOfYear;
        return daysDifference;
    }
}