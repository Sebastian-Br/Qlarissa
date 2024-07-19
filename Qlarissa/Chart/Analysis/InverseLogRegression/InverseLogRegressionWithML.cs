using MathNet.Numerics;
using Qlarissa.Chart.Enums;
using Qlarissa.ErrorCorrection;

namespace Qlarissa.Chart.Analysis.InverseLogRegression
{
    public class InverseLogRegressionWithML : IRegressionResult
    {
        public InverseLogRegressionWithML(Symbol symbol)
        {
            BaseModel = symbol.InverseLogRegressionModel;

            SymbolDataPoint[] dataPoints = symbol.GetDataPointsForAnalysis();
            double[] Xs = dataPoints.Select(dataPoint => dataPoint.Date.ToDouble()).ToArray();
            double[] Ys = dataPoints.Select(dataPoint => dataPoint.MediumPrice).ToArray();
            SymbolDataPoint mock = new();
            mock.MediumPrice = 1.0; mock.Date = new DateOnly(3322, 1, 1);
            ConstModelInputParameters = new(BaseModel, expected: mock, actual: mock, trainingPeriodDataPoints: dataPoints);
            LastTrainingPeriodDay = dataPoints.Last().Date;
            Rsquared = GoodnessOfFit.RSquared(Xs.Select(x => GetEstimate(x)), Ys);
            //^only the DaysSinceEndOfTrainingPeriod property can not be set here since this relates to 
        }

        InverseLogRegressionResult BaseModel {  get; set; }

        double Rsquared { get; set; }

        ErrorCorrectionForInvLogFrame ConstModelInputParameters { get; set; }

        DateOnly LastTrainingPeriodDay { get; set; }

        public DateOnly GetCreationDate()
        {
            throw new NotImplementedException();
        }

        public double GetEstimate(DateOnly date)
        {
            return GetEstimate(date.ToDouble());
        }

        public double GetEstimate(double t)
        {
            RegressionResultType baseModelbaseRegressionType = BaseModel.GetEffectiveInnerRegression().GetRegressionResultType();
            double estimateDeviationPercentage = 0;
            switch (baseModelbaseRegressionType)
            {
                case RegressionResultType.Logistic:
                    MLModel_INVLOG_LogBase.ModelInput logModelInput = new();
                    logModelInput.P0 = (float)ConstModelInputParameters.P0;
                    logModelInput.P1 = (float)ConstModelInputParameters.P1;
                    logModelInput.P2 = (float)ConstModelInputParameters.P2;
                    logModelInput.RSquared = (float)ConstModelInputParameters.RSquared;
                    logModelInput.SlopeOfOuterFunctionAtEndOfTrainingPeriod = (float)ConstModelInputParameters.SlopeOfOuterFunctionAtEndOfTrainingPeriod;
                    logModelInput.TrainingPeriodDays = GetDaysDifferenceFromStartOfTrainingPeriod(t);
                    estimateDeviationPercentage = MLModel_INVLOG_LogBase.Predict(logModelInput).Score;
                    break;
                case RegressionResultType.Exponential:
                    MLModel_INVLOG_ExpBase.ModelInput expModelInput = new();
                    expModelInput.P0 = (float)ConstModelInputParameters.P0;
                    expModelInput.P1 = (float)ConstModelInputParameters.P1;
                    expModelInput.P2 = (float)ConstModelInputParameters.P2;
                    expModelInput.RSquared = (float)ConstModelInputParameters.RSquared;
                    expModelInput.SlopeOfOuterFunctionAtEndOfTrainingPeriod = (float)ConstModelInputParameters.SlopeOfOuterFunctionAtEndOfTrainingPeriod;
                    expModelInput.TrainingPeriodDays = GetDaysDifferenceFromStartOfTrainingPeriod(t);
                    estimateDeviationPercentage = MLModel_INVLOG_ExpBase.Predict(expModelInput).Score;
                    break;
                case RegressionResultType.Linear:
                    MLModel_INVLOG_LinBase.ModelInput linearModelInput = new();
                    linearModelInput.P0 = (float)ConstModelInputParameters.P0;
                    linearModelInput.P1 = (float)ConstModelInputParameters.P1;
                    linearModelInput.P2 = (float)ConstModelInputParameters.P2;
                    linearModelInput.RSquared = (float)ConstModelInputParameters.RSquared;
                    linearModelInput.SlopeOfOuterFunctionAtEndOfTrainingPeriod = (float)ConstModelInputParameters.SlopeOfOuterFunctionAtEndOfTrainingPeriod;
                    linearModelInput.TrainingPeriodDays = GetDaysDifferenceFromStartOfTrainingPeriod(t);
                    estimateDeviationPercentage = MLModel_INVLOG_LinBase.Predict(linearModelInput).Score;
                    break;
            }

            double factor_ExpectedOverActual = 1.0 + (estimateDeviationPercentage / 100.0);
            double expected = BaseModel.GetEstimate(t);
            double mlAdjustedPrediction = expected / factor_ExpectedOverActual;
            return mlAdjustedPrediction;
        }

        public List<double> GetParameters()
        {
            throw new NotImplementedException();
        }

        public RegressionResultType GetRegressionResultType()
        {
            throw new NotImplementedException();
        }

        public double GetRsquared()
        {
            return Rsquared;
        }

        public double GetWeight()
        {
            throw new NotImplementedException();
        }

        int GetDaysDifferenceFromStartOfTrainingPeriod(double targetDate)
        {
            DateOnly targetDateAsDateOnly = DateOnlyExtensions.FromDouble(targetDate);
            return GetExactDaysDifference(LastTrainingPeriodDay, targetDateAsDateOnly);
        }

        static int GetExactDaysDifference(DateOnly startDate, DateOnly endDate)
        {
            if (endDate < startDate)
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
    }
}