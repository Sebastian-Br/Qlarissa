# Qlarissa - The Quantitative Finance Assistant
</br>
This tool enables its users to rapidly:
</br>

- Screen assets such as stocks or ETFs
- Quantify their risk-reward profile
- Visualize and store analysis results
</br>
By:
</br>

- Retrieving up-to-date asset data via a local Python/yFinance API (script contained in repository)
- Performing regression analyses to model long-term trends
  - Includes a custom-made regression function to more accurately reflect changes in growth
- Generating graphs to visualize regression functions, growth- and risk-profiles
  - Can visualize the comparative risk-reward profile of leveraged certificates vs. the underlying asset
- Ranking assets based on long term growth, fundamental data, and analyst sentiment

</br>
This image showcases how the tool displays stock data and the regression functions it uses to quantify long-term trends.
</br>
It projects the long-term trend into the future, adding dividend payouts, to reflect the 1-year and 3-year growth expectation.
</br>
Trends are one, but far from the only factor that the tool considers when ranking assets.
</br>

![JNJ](https://github.com/user-attachments/assets/61b0e81d-099a-4294-bbed-a92000594870)

</br>
</br>

Qlarissa quantifies growth-profiles of assets. From this image for instance, the user may observe that a 5-10% annualized growth rate had historically been the most likely outcome (at 29.4%) for Johnson & Johnson over all analyzed 24-month intervals.
</br>

![JNJ_Growth24](https://github.com/user-attachments/assets/0d52cad8-2bdb-46f2-a5fc-c6e39e83d93c)

</br>

Qlarissa can quantify risk profiles of assets. From the below graph, users may ascertain that, for an arbitrary 24-month period, the likelihood of Johnson & Johnson stock not falling below 20% of its initial value (at the time it had been purchased) had been 83.92%.

![JNJ_MaxLoss24](https://github.com/user-attachments/assets/656a99b4-36f6-45ff-a90d-d6c473262153)


</br>
Qlarissa quantifies the risk-reward profile of leveraged certificates vs. the underlying asset. A user may observe from the below graph that there is no leveraged certificate of any leverage between 1.1 and 4 that had historically outperformed the stock.

![JNJ_LeveragedOverperformance24](https://github.com/user-attachments/assets/d6f5f9c0-4bc2-4514-b564-9c2813ccc897)
