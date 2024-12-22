from flask import Flask, request, jsonify
import yfinance as yf
import pandas as pd

app = Flask(__name__)

@app.route('/stock_data', methods=['GET'])
def get_stock_data():
    ticker = request.args.get('ticker')
    start_date = request.args.get('start_date')
    end_date = request.args.get('end_date')

    if ticker:
        if start_date and end_date:
            data = yf.download(ticker, start=start_date, end=end_date)

        if isinstance(data, pd.DataFrame) and not data.empty:
            # Convert timestamps to string format with date only
            data.index = data.index.strftime('%Y-%m-%d')
            # Get the P/E ratio information
            ticker_info = yf.Ticker(ticker).info
            #print(ticker_info)
            # Get/format dividend history
            ticker_dividend_history = yf.Ticker(ticker).dividends
            ticker_dividend_history.index = ticker_dividend_history.index.strftime('%Y-%m-%d')
            # Exclude keys from historical data
            data_filtered = data.drop(columns=["Adj Close", "Volume"])
            #earnings_history = yf.Ticker(ticker).earnings_history
            #earnings_history.index = earnings_history.index.strftime('%Y-%m-%d')
            quarterly_income_statement_df = yf.Ticker(ticker).quarterly_income_stmt
            tmp = quarterly_income_statement_df.to_dict(orient='dict')
            quarterly_income_statement_dictionary = {str(date): tmp[date] for date in sorted(tmp)}
            print(quarterly_income_statement_dictionary)
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
                "HistoricalData": data_filtered.to_dict(orient='index'),
                "DividendHistory": ticker_dividend_history.to_dict(),
                "TargetMeanPrice": ticker_info.get('targetMeanPrice', '0'),
                "NumberOfAnalystOpinions": ticker_info.get('numberOfAnalystOpinions', '0'),
                "InvestorRelationsWebsite": ticker_info.get('irWebsite', '0'),
                "SharesOutstanding": ticker_info.get('sharesOutstanding', '0'),
                "RecentFourQuartersIncomeStatements": quarterly_income_statement_dictionary
            }
            return jsonify(response)
        else:
            return jsonify({"error": "No data available for the specified parameters"})
    else:
        return jsonify({"error": "Ticker symbol not provided"})

if __name__ == '__main__':
    app.run(debug=True)