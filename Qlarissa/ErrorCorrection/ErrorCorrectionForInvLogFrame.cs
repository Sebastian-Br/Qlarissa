using Qlarissa.Chart;
using Qlarissa.Chart.Analysis;
using Qlarissa.Chart.Analysis.InverseLogRegression;
using Qlarissa.Chart.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qlarissa.ErrorCorrection
{
    public class ErrorCorrectionForInvLogFrame
    {
        public ErrorCorrectionForInvLogFrame(InverseLogRegressionResult inverseLogModel, SymbolDataPoint expected, SymbolDataPoint actual, SymbolDataPoint lastDataPointInTrainingPeriod)
        {
            RSquared = inverseLogModel.GetRsquared();
            IRegressionResult innerRegression = inverseLogModel.GetEffectiveInnerRegression();
            InnerRegressionType = innerRegression.GetRegressionResultType();

            EstimateDifferencePercentage = 100.0 * ((expected.MediumPrice / actual.MediumPrice) - 1.0);
            DaysSinceEndOfTrainingPeriod = GetExactDaysDifference(lastDataPointInTrainingPeriod.Date, actual.Date);
        }

        public double RSquared { get; private set; }

        public RegressionResultType InnerRegressionType { get; private set; }

        public int DaysSinceEndOfTrainingPeriod { get; private set; }

        /// <summary>
        /// If the model overestimates the actual data by 10%, this value would be 10.
        /// </summary>
        public double EstimateDifferencePercentage { get; private set; }

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
}