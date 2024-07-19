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

    public static DateOnly FromDouble(double yearIndex)
    {
        int year = (int)Math.Floor(yearIndex);
        double fractionOfYear = yearIndex - year;
        int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
        int dayOfYear = (int)Math.Round(fractionOfYear * daysInYear);

        // Ensure dayOfYear is within the valid range
        if (dayOfYear < 1)
        {
            dayOfYear = 1;
        }
        else if (dayOfYear > daysInYear)
        {
            dayOfYear = daysInYear;
        }

        return new DateOnly(year, 1, 1).AddDays(dayOfYear - 1);
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