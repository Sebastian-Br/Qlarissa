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
            # Get/format dividend history
            ticker_dividend_history = yf.Ticker(ticker).dividends
            ticker_dividend_history.index = ticker_dividend_history.index.strftime('%Y-%m-%d')
            # Exclude keys from historical data
            data_filtered = data.drop(columns=["Adj Close", "Close", "Open", "Volume"])
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
                "DividendHistory": ticker_dividend_history.to_dict()
            }
            return jsonify(response)
        else:
            return jsonify({"error": "No data available for the specified parameters"})
    else:
        return jsonify({"error": "Ticker symbol not provided"})

if __name__ == '__main__':
    app.run(debug=True)