using Qlarissa.Chart.Enums;

namespace Qlarissa.Chart.Analysis.InverseLogRegression
{
    public class InverseLogRegressionWithML : IRegressionResult
    {
        public InverseLogRegressionWithML(InverseLogRegressionResult baseModel)
        {
            BaseModel = baseModel;
        }

        InverseLogRegressionResult BaseModel {  get; set; }

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
                    estimateDeviationPercentage = MLModel_INVLOG_LogBase.Predict(logModelInput).EstimateDeviationPercentage;
                    break;
                case RegressionResultType.Exponential:
                    MLModel_INVLOG_ExpBase.ModelInput expModelInput = new();
                    estimateDeviationPercentage = MLModel_INVLOG_ExpBase.Predict(expModelInput).EstimateDeviationPercentage;
                    break;
                case RegressionResultType.Linear:
                    MLModel_INVLOG_LinBase.ModelInput linearModelInput = new();
                    estimateDeviationPercentage = MLModel_INVLOG_LinBase.Predict(linearModelInput).EstimateDeviationPercentage;
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
            throw new NotImplementedException();
        }

        public double GetWeight()
        {
            throw new NotImplementedException();
        }
    }
}