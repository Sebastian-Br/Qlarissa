using System;

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