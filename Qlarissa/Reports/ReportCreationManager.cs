using Qlarissa.Chart.Ranking;
using Qlarissa.CustomConfiguration;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qlarissa.Reports;

internal class ReportCreationManager
{
    public void GenerateReport(List<SymbolWithAggregatedScore> symbolsWithScores)
    {
        var report = new Report(symbolsWithScores);
        report.GeneratePdf(SaveLocationsConfiguration.GetReportSaveFileLocation());
        var longReport = new LongReport(symbolsWithScores);
        longReport.GeneratePdf(SaveLocationsConfiguration.GetLongReportSaveFileLocation());
    }
}