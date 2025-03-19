using MathNet.Numerics;
using Qlarissa.Chart.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static Plotly.NET.StyleParam.LinearAxisId;

namespace Qlarissa.Chart.Analysis.BaseRegressions;

class LogCappedRegressionResult : IRegressionResult
{
    /// <summary>
    /// y(t) = (-ln((t-t0)/b))k(t-t0)
    //                 /                        +   C
    //                 b
    // Preprocessing will occur internally!!
    /// </summary>
    public LogCappedRegressionResult(double[] Xs, double[] Ys)
    {
        Parameters = new();
        double t0 = Xs[0];
        Parameters.Add(t0);
        double[] preprocessedXs = new double[Xs.Length];
        for (int i = 0; i < Xs.Length; i++)
        {
            preprocessedXs[i] = Xs[i] - t0;
        }

        (double k, double b, double C) = Fit(preprocessedXs, Ys, Xs);
        Parameters.Add(k);
        Parameters.Add(b);
        Parameters.Add(C);
        DateCreated = DateOnly.FromDateTime(DateTime.Now);
    }

    List<double> Parameters { get; set; }

    DateOnly DateCreated { get; set; }

    double Rsquared { get; set; }

    public DateOnly GetCreationDate()
    {
        return DateCreated;
    }

    public double GetEstimate(DateOnly date)
    {
        double t = date.ToDouble();
        return GetEstimate(t);
    }

    /// <summary>
    /// y(t) = (-ln((t-t0)/b))k(t-t0)
    //                 /                        +   C
    //                 b
    /// </summary>
    public double GetEstimate(double t)
    {
        if (t < Parameters[0])
            return Parameters[3]; // C

        //                          t0                   b              k                       t0
        return (((-Math.Log((t - Parameters[0])/ Parameters[2])) * Parameters[1] * (t - Parameters[0]))
                / Parameters[2]) // b
                + Parameters[3]; // C
    }

    private double GetInternalEstimate(double t, double k, double b, double C)
    {
        if (t <= Parameters[0])
            return C; // C

        double result = ((-Math.Log((t - Parameters[0]) / b) * k * (t - Parameters[0]))
                / b) // b
                + C; // C

        return result;
    }

    private double GetXCoordinateOfMaximum(double b)
    {
        return b / Math.E;
    }

    private double GetYCoordinateOfMaximum(double k, double C)
    {
        return (k / Math.E) + C;
    }

    private double GivenYmaxAndC_DetermineK(double ymax, double C)
    {
        return Math.E * (ymax - C);
    }

    private double GivenXmax_DetermineB(double xmax)
    {
        return Math.E * xmax;
    }

    public List<double> GetParameters()
    {
        return Parameters;
    }

    public RegressionResultType GetRegressionResultType()
    {
        return RegressionResultType.LogisticallyCapped;
    }

    public double GetRsquared()
    {
        return Rsquared;
    }

    public double GetWeight()
    {
        throw new NotImplementedException();
    }

    (double, double, double) Fit(double[] internallyProcessedXs, double[] Ys, double[] originalXs)
    { // Order: k, b, C
        if (internallyProcessedXs.Length != Ys.Length)
        {
            throw new InvalidOperationException("Arrays need to be of the same length");
        }

        // hypersuperduperefficient estimation of initial regression parameters
        double initialGuessYmax = GetAverageOfLast750Elements(Ys);
        double b = GivenXmax_DetermineB(internallyProcessedXs[^1]);
        double C = GetAverageOfFirst30Elements(Ys);
        double k = GivenYmaxAndC_DetermineK(initialGuessYmax, C);

        int currentIteration = 0;
        int maxIterations = 200000;

        double db = 0.001; // initial step sizes for b or k
        double dk = 0.001;

        double currentBestRsquared = GoodnessOfFit.RSquared(originalXs.Select(t => GetInternalEstimate(t, k, b, C)), Ys);

        double recentBestB = b;
        double recentBestK = k;

        while(currentIteration < maxIterations)
        {
            double testB = b + db;
            double testK = k + dk;

            double testB_rsquared = GoodnessOfFit.RSquared(originalXs.Select(t => GetInternalEstimate(t, k, testB, C)), Ys);
            double testK_rsquared = GoodnessOfFit.RSquared(originalXs.Select(t => GetInternalEstimate(t, testK, b, C)), Ys);
            double bImprovement = testB_rsquared - currentBestRsquared;
            double kImprovement = testK_rsquared - currentBestRsquared;

            if(bImprovement > 0 || kImprovement > 0)
            {
                if (bImprovement > 0  && kImprovement > 0) // decide between stepping in the k or b direction
                {
                    
                }
                else // only changing one of k or b improves the regression
                {

                }
            }
            else // we need to backtrack as the last regression step could not improve the parameters
            {

            }

            break;
            currentIteration++;
        }

        Rsquared = currentBestRsquared;
        return (k, b, C);
    }

    double GetAverageOfFirst30Elements(double[] Ys)
    {
        double sum = 0;
        for(int i = 0; i < 30; i++)
        {
            sum += Ys[i];
        }

        return sum / 30.0;
    }

    double GetAverageOfLast750Elements(double[] Ys) //~last 3 years
    {
        double sum = 0;
        int length = Ys.Length;

        if (length < 750)
            throw new ArgumentException("Array must contain at least 750 elements");

        for (int i = length - 750; i < length; i++)
        {
            sum += Ys[i];
        }

        return sum / 750.0;
    }

    /// <summary>
    /// Tracks the last 10 choices between b and k
    /// </summary>
    /// <param name="isK"></param>
    /// <param name="operations"></param>
    static void AddOperation(bool isK, Queue<bool> operations)
    {
        if (operations.Count >= 10)
        {
            operations.Dequeue();
        }
        operations.Enqueue(isK);
    }

    static int GetKCount(Queue<bool> operations)
    {
        int ks = 0;
        foreach (bool isK in operations)
        {
            if (isK)
                ks++;
        }

        return ks;
    }
}