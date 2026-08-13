namespace Qlarissa.Chart.Ranking;

internal record SymbolWithAggregatedScore
{
    public SymbolWithAggregatedScore(Symbol symbol, double aggregatedScore)
    {
        Symbol = symbol;
        AggregatedScore = aggregatedScore;
    }

    public Symbol Symbol { get; init; }
    public double AggregatedScore { get; init; }
    public int Rank { get; set; }

}