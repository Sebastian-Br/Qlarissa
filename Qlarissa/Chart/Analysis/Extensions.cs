public static class DateOnlyExtensions
{
    public static double ToDouble(this DateOnly date)
    {
        int year = date.Year;
        int dayOfYear = date.DayOfYear;
        int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
        double yearIndex = year + dayOfYear / (double)daysInYear;
        return yearIndex;
    }
}

public static class GoodnessOfFitExtensions
{
    public static double CalculateRMSE(double[] expected, double[] actual)
    {
        if (expected.Length != actual.Length)
        {
            throw new ArgumentException("The length of the arrays must be the same.");
        }

        double sumSquaredErrors = 0.0;

        for (int i = 0; i < expected.Length; i++)
        {
            double error = expected[i] - actual[i];
            sumSquaredErrors += error * error;
        }

        double mse = sumSquaredErrors / expected.Length;
        double rmse = Math.Sqrt(mse);

        return rmse;
    }

}