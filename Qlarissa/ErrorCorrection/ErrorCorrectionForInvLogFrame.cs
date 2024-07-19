﻿using Qlarissa.Chart;
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
        public ErrorCorrectionForInvLogFrame(InverseLogRegressionResult inverseLogModel, SymbolDataPoint expected, SymbolDataPoint actual, SymbolDataPoint[] trainingPeriodDataPoints)
        {
            RSquared = inverseLogModel.GetRsquared();
            IRegressionResult innerRegression = inverseLogModel.GetEffectiveInnerRegression();
            InnerRegressionType = innerRegression.GetRegressionResultType();
            SlopeOfOuterFunctionAtEndOfTrainingPeriod = GetSlopeAtDate(inverseLogModel, trainingPeriodDataPoints.Last().Date);

            OuterModel = inverseLogModel;

            EstimateDeviationPercentage = 100.0 * ((expected.MediumPrice / actual.MediumPrice) - 1.0);
            DaysSinceEndOfTrainingPeriod = GetExactDaysDifference(trainingPeriodDataPoints.Last().Date, actual.Date);
            TrainingPeriodDays = GetExactDaysDifference(trainingPeriodDataPoints[0].Date, trainingPeriodDataPoints.Last().Date);

            List<double> baseModelParameters = OuterModel.GetEffectiveInnerRegression().GetParameters();
            P0 = baseModelParameters[0];
            P1 = baseModelParameters[1];
            P2 = baseModelParameters[2];
        }

        public double RSquared { get; private set; }

        public RegressionResultType InnerRegressionType { get; private set; }

        public double SlopeOfOuterFunctionAtEndOfTrainingPeriod { get; private set; }

        public int TrainingPeriodDays { get; private set; }

        public int DaysSinceEndOfTrainingPeriod { get; private set; }

        private InverseLogRegressionResult OuterModel { get; set; }

        public double P0 { get; private set; }
        public double P1 { get; private set; }
        public double P2 { get; private set; }

        /// <summary>
        /// If the model overestimates the actual data by 10%, this value would be 10.
        /// </summary>
        public double EstimateDeviationPercentage { get; private set; }

        static int GetExactDaysDifference(DateOnly startDate, DateOnly endDate)
        {
            if(endDate < startDate)
            {
                return -GetExactDaysDifference(startDate: endDate, endDate: startDate);
            }

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

        double GetSlopeAtDate(InverseLogRegressionResult model, DateOnly date)
        {
            double epsilon = 1e-3;
            double slope = 0;
            double dateAsDouble = date.ToDouble();

            double dateMinusEpsilon = dateAsDouble - epsilon;
            double datePlusEpsilon = dateAsDouble + epsilon;
            double dx = 2 * epsilon;
            double dy = model.GetEstimate(datePlusEpsilon) - model.GetEstimate(dateMinusEpsilon);
            slope = dy / dx;

            return slope;
        }

        public static string GetCsvHeader(RegressionResultType regressionResultType)
        {
            string csvHeader = nameof(RSquared) + "," + nameof(SlopeOfOuterFunctionAtEndOfTrainingPeriod) + 
                ","  + nameof(TrainingPeriodDays);

            // at the moment, all 3 base regression types have 3 Parameters. Otherwise the below line would have to be specific to the number of parameters used in that base regression.
            csvHeader += ",P0,P1,P2";

            csvHeader += "," + nameof(EstimateDeviationPercentage) + "\n";
            return csvHeader;
        }

        public string AsCsvRow()
        {
            string csvRow = RSquared + "," + SlopeOfOuterFunctionAtEndOfTrainingPeriod +
                "," + TrainingPeriodDays + "," + P0 + "," + P1 + "," + P2 +
                "," + EstimateDeviationPercentage + "\n";
            return csvRow;
        }
    }
}