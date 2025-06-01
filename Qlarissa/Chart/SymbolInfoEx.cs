namespace Qlarissa.Chart;

public class SymbolInfoEx
{
    public SymbolInfoEx() { }

    public double TrailingPE { get; set; }

    public double ForwardPE { get; set; }

    /// <summary>
    /// The mean target price in 1 year's time as predicted by analysts
    /// </summary>
    public double TargetMeanPrice { get; set; }

    public int NumberOfAnalystOpinions { get; set; }

    public double RecommendationMean {  get; set; }
}