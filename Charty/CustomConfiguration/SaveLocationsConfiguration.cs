using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charty.CustomConfiguration
{
    public class SaveDirectoriesConfiguration
    {
        public string ChartsDirectory { get; private set; }

        public string VolatilityAnalysisDirectory { get; private set; }

        private string BaseDirectory { get; set; }

        public SaveDirectoriesConfiguration()
        {
            BaseDirectory = "ChartyData/";

            ChartsDirectory = BaseDirectory + "Charts/";
            VolatilityAnalysisDirectory = BaseDirectory + "VolatilityAnalysis/";

            CreatePathIfNotExists(ChartsDirectory);
            CreatePathIfNotExists(VolatilityAnalysisDirectory);
        }

        private void CreatePathIfNotExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
    }
}