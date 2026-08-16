using MathNet.Numerics;
using Qlarissa.Chart.Ranking;
using Qlarissa.CustomConfiguration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qlarissa.Reports;

internal class Report(List<SymbolWithAggregatedScore> symbolsWithScores) : IDocument
{
    private List<SymbolWithAggregatedScore> SymbolsWithScores { get; set; } = symbolsWithScores;
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public void Compose(IDocumentContainer container)
    {
        foreach (var symbol in SymbolsWithScores)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0.5f, Unit.Centimetre);
                page.PageColor(Colors.BlueGrey.Medium); // transparent is default
                page.Content().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem(1)
                            .Text($"{symbol.Rank} - {symbol.Symbol.Overview.Name} ({symbol.Symbol.Overview.Symbol})")
                            .FontSize(20)
                            .Bold();

                        row.RelativeItem(1)
                            .AlignCenter()
                            .AlignMiddle()
                            .Text($"Score: {symbol.AggregatedScore.Round(1)}")
                            .FontSize(15);
                    });

                    column.Item()
                        .PaddingTop(10)
                        .Text(symbol.Symbol.Overview.Description)
                        .FontSize(10);

                    column.Item()
                    .PaddingTop(10)
                    .Image(SaveLocationsConfiguration.GetSymbolChartSaveFileLocation(symbol.Symbol));
                });
            });
        }
    }
}