from flask import Flask, request, jsonify
import yfinance as yf
import pandas as pd
import numpy as np
import scipy

app = Flask(__name__)

@app.route('/stock_data', methods=['GET'])
def get_stock_data():
    ticker = request.args.get('ticker')
    start_date = request.args.get('start_date')
    end_date = request.args.get('end_date')

    if ticker:
        if start_date and end_date:
            #GetPEData(ticker)
            data = yf.download(ticker, start=start_date, end=end_date)

        if isinstance(data, pd.DataFrame) and not data.empty:
            # Convert timestamps to string format with date only
            data.index = data.index.strftime('%Y-%m-%d')
            # Get the P/E ratio information
            ticker_info = yf.Ticker(ticker).info
            print(yf.Ticker(ticker).recommendations_summary)
            print(yf.Ticker(ticker))
            # Get/format dividend history
            ticker_dividend_history = yf.Ticker(ticker).dividends
            ticker_dividend_history.index = ticker_dividend_history.index.strftime('%Y-%m-%d')
            # Exclude keys from historical data
            #print(list(data.columns))
            # Drop "Volume" and flatten MultiIndex columns
            data_filtered = data.droplevel(axis=1, level=1) if isinstance(data.columns, pd.MultiIndex) else data
            data_filtered = data_filtered.drop(columns=["Volume"], errors="ignore")

            # Convert to dictionary with correct format
            data_filtered_dict = data_filtered.to_dict(orient='index')
            #print(type(data_filtered.index))
            #print(data_filtered.to_dict(orient='index'))
            #earnings_history = yf.Ticker(ticker).earnings_history
            #earnings_history.index = earnings_history.index.strftime('%Y-%m-%d')
            #quarterly_income_statement_df = yf.Ticker(ticker).quarterly_income_stmt
            #tmp = quarterly_income_statement_df.to_dict(orient='dict')
            #quarterly_income_statement_dictionary = {str(date): tmp[date] for date in sorted(tmp)}
            #print(quarterly_income_statement_dictionary)
            #earnings.index = earnings.index.strftime('%Y-%m-%d')
            #print(earnings)
            # Format earnings data for JSON
            #if not quarterly_earnings.empty:
                #quarterly_earnings.index = quarterly_earnings.index.strftime('%Y-%m-%d')
                #quarterly_earnings_data = quarterly_earnings.to_dict(orient='index')
            #else:
                #quarterly_earnings_data = {}
            
            # Construct JSON response including SymbolOverview fields and filtered historical stock data
            response = {
                "Symbol": ticker,
                "Name": ticker_info.get('longName', 'UNKNOWN'),
                "Currency": ticker_info.get('currency', 'USD'),
                "MarketCapitalization": ticker_info.get('marketCap', '0'),
                "TrailingPE": ticker_info.get('trailingPE', '0'),
                "ForwardPE": ticker_info.get('forwardPE', '0'),
                "DividendPerShareYearly": ticker_info.get('dividendRate', '0'),
                "DividendHistory": ticker_dividend_history.to_dict(),
                "TargetMeanPrice": ticker_info.get('targetMeanPrice', '0'),
                "NumberOfAnalystOpinions": ticker_info.get('numberOfAnalystOpinions', '0'),
                "InvestorRelationsWebsite": ticker_info.get('irWebsite', '0'),
                #"SharesOutstanding": ticker_info.get('sharesOutstanding', '0'),
                "RecommendationMean": ticker_info.get('recommendationMean', '0'),
                #"RecentFourQuartersIncomeStatements": quarterly_income_statement_dictionary,
                "HistoricalData": data_filtered_dict,
                "ComputedRecommendationMean": compute_recommendation_mean_last_2m(yf.Ticker(ticker).recommendations_summary),
            }
            return jsonify(response)
        else:
            return jsonify({"error": "No data available for the specified parameters"})
    else:
        return jsonify({"error": "Ticker symbol not provided"})

    #https://medium.com/@tballz/retrieving-historical-p-e-data-with-python-d09198335984
def GetPEData(symbol):
    data = pd.read_html(f'http://macrotrends.net/stocks/charts/MSFT/microsoft/pe-ratio', skiprows=1)
    df = pd.DataFrame(data[0])
    df = df.columns.to_frame().T.append(df, ignore_index=True)
    df.columns = range(len(df.columns))
    df = df[1:]
    df = df.rename(columns={0: 'Date', 1: 'Price', 2: 'EPS', 3: 'PE'})
    df['EPS'][1] = ''
    df.set_index('Date', inplace = True)
    df = df.sort_index()
    df['trend'] = ""
    df['PE'] = df['PE'].astype(float)
    print(df['PE'].mean())
    return df['PE'].mean()

def compute_recommendation_mean_last_2m(df: pd.DataFrame) -> float | None:
    if df.empty:
        return 0 # BE handles this case and re assigns a sensible value. This is for ETFs.
    weights = {
        "strongBuy": 1.0,
        "buy": 2.0,
        "hold": 3.0,
        "sell": 4.0,
        "strongSell": 5.0,
    }

    cols = list(weights.keys())

    recent = df[df["period"].isin(["0m", "-1m"])]

    total_ratings = recent[cols].sum().sum()
    if total_ratings == 0:
        return None

    weighted_sum = (
        recent[cols]
        .mul(pd.Series(weights))
        .sum()
        .sum()
    )

    return weighted_sum / total_ratings

if __name__ == '__main__':
    app.run(debug=True)