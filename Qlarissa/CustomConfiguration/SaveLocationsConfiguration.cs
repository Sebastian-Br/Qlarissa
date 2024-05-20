using Qlarissa.Chart;
using Qlarissa.Chart.ChartAnalysis.GrowthVolatilityAnalysis;
using Qlarissa.Chart.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qlarissa.CustomConfiguration
{
    public static class SaveLocationsConfiguration
    {
        private static string BaseDirectory = "_Data/";

        private static string ChartsDirectory = BaseDirectory + "Charts/";

        private static string VolatilityAnalysisDirectory = BaseDirectory + "VolatilityAnalysis/";

        private static string ErrorAnalysisDirectory = BaseDirectory + "ErrorCorrection/";

        private static string _MachineLearningDataDirectory = ErrorAnalysisDirectory + "_MachineLearning/";

        public static string GetSymbolChartSaveFileLocation(Symbol symbol)
        {
            string Directory = ChartsDirectory + symbol.Overview.Symbol + "/";
            CreateDirectoryIfNotExists(Directory);
            string FileName = symbol.Overview.Symbol + ".png";
            return Directory + FileName;
        }

        public static string GetLogRegressionsSaveFileLocation(Symbol symbol)
        {
            string Directory = ChartsDirectory + symbol.Overview.Symbol + "/";
            CreateDirectoryIfNotExists(Directory);
            string FileName = symbol.Overview.Symbol + "_LogRegressions.png";
            return Directory + FileName;
        }

        public static string GetGrowthAnalysisSaveFileLocation(Symbol symbol, GrowthVolatilityAnalysis gva)
        {
            string Directory = VolatilityAnalysisDirectory + symbol.Overview.Symbol + "/";
            CreateDirectoryIfNotExists(Directory);
            string FileName = symbol.Overview.Symbol + "_Growth" + (int)gva.TimePeriod  + ".png";
            return Directory + FileName;
        }

        public static string GetLeveragedOverperformanceAnalysisSaveFileLocation(Symbol symbol, GrowthVolatilityAnalysis gva)
        {
            string Directory = VolatilityAnalysisDirectory + symbol.Overview.Symbol + "/";
            CreateDirectoryIfNotExists(Directory);
            string FileName = symbol.Overview.Symbol + "_LeveragedOverperformance" + (int)gva.TimePeriod + ".png";
            return Directory + FileName;
        }

        public static string GetMaxLossAnalysisSaveFileLocation(Symbol symbol, GrowthVolatilityAnalysis gva)
        {
            string Directory = VolatilityAnalysisDirectory + symbol.Overview.Symbol + "/";
            CreateDirectoryIfNotExists(Directory);
            string FileName = symbol.Overview.Symbol + "_MaxLoss" + (int)gva.TimePeriod + ".png";
            return Directory + FileName;
        }

        public static string GetErrorAnalysisCombinedHeatmapSaveFileLocation(Symbol symbol)
        {
            string Directory = ErrorAnalysisDirectory + symbol.Overview.Symbol + "/";
            CreateDirectoryIfNotExists(Directory);
            string FileName = symbol.Overview.Symbol + "_Combined_ErrorHeatmap" + ".png";
            return Directory + FileName;
        }

        public static string GetErrorAnalysis_ExpBaseRegression_HeatmapSaveFileLocation(Symbol symbol)
        {
            string Directory = ErrorAnalysisDirectory + symbol.Overview.Symbol + "/";
            CreateDirectoryIfNotExists(Directory);
            string FileName = symbol.Overview.Symbol + "_ExpBaseReg_ErrorHeatmap" + ".png";
            return Directory + FileName;
        }

        public static string GetErrorAnalysis_LinBaseRegression_HeatmapSaveFileLocation(Symbol symbol)
        {
            string Directory = ErrorAnalysisDirectory + symbol.Overview.Symbol + "/";
            CreateDirectoryIfNotExists(Directory);
            string FileName = symbol.Overview.Symbol + "_LinBaseReg_ErrorHeatmap" + ".png";
            return Directory + FileName;
        }

        public static string GetErrorAnalysis_LogBaseRegression_HeatmapSaveFileLocation(Symbol symbol)
        {
            string Directory = ErrorAnalysisDirectory + symbol.Overview.Symbol + "/";
            CreateDirectoryIfNotExists(Directory);
            string FileName = symbol.Overview.Symbol + "_LogBaseReg_ErrorHeatmap" + ".png";
            return Directory + FileName;
        }

        public static string GetPredictionErrorCSV_SaveFileLocation_ForINVLOG_ByBaseRegressionType(RegressionResultType BaseRegression)
        {
            string Directory = _MachineLearningDataDirectory;
            CreateDirectoryIfNotExists(Directory);
            string FileName;

            switch (BaseRegression)
            {
                case RegressionResultType.Logistic:
                    FileName = "PredictionErrorsFor_INVLOG_Log_Base.csv";
                    break;
                case RegressionResultType.Linear:
                    FileName = "PredictionErrorsFor_INVLOG_Lin_Base.csv";
                    break;
                case RegressionResultType.Exponential:
                    FileName = "PredictionErrorsFor_INVLOG_Exp_Base.csv";
                    break;
                default:
                    throw new NotImplementedException("Unknown Base Regression Type for INVLOG: " + BaseRegression);
            }

            return Directory + FileName;
        }

        public static string GetPredictionErrorPNG_SaveFileLocation_ForINVLOG_ByBaseRegressionType(RegressionResultType BaseRegression)
        {
            string Directory = _MachineLearningDataDirectory;
            CreateDirectoryIfNotExists(Directory);
            string FileName;

            switch (BaseRegression)
            {
                case RegressionResultType.Logistic:
                    FileName = "PredictionErrorsFor_INVLOG_Log_Base.png";
                    break;
                case RegressionResultType.Linear:
                    FileName = "PredictionErrorsFor_INVLOG_Lin_Base.png";
                    break;
                case RegressionResultType.Exponential:
                    FileName = "PredictionErrorsFor_INVLOG_Exp_Base.png";
                    break;
                default:
                    throw new NotImplementedException("Unknown Base Regression Type for INVLOG: " + BaseRegression);
            }

            return Directory + FileName;
        }

        public static string GetPredictionErrorPNG_SaveFileLocation_ForINVLOG_AllRegressionTypes()
        {
            string Directory = _MachineLearningDataDirectory;
            CreateDirectoryIfNotExists(Directory);
            string FileName = "PredictionErrorsFor_INVLOG_AllBases.png";
            return Directory + FileName;
        }

        private static void CreateDirectoryIfNotExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}