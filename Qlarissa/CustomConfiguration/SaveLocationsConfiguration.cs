using Qlarissa.Chart;
using Qlarissa.Chart.ChartAnalysis.GrowthVolatilityAnalysis;
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

        public static string GetErrorAnalysisHeatmapSaveFileLocation(Symbol symbol)
        {
            string Directory = ErrorAnalysisDirectory + symbol.Overview.Symbol + "/";
            CreateDirectoryIfNotExists(Directory);
            string FileName = symbol.Overview.Symbol + "_ErrorHeatmap" + ".png";
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