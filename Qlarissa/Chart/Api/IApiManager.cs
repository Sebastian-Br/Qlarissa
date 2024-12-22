namespace Qlarissa.Chart.Api;

internal interface IApiManager
{
    public Task<Symbol> RetrieveSymbol(string symbol);
}