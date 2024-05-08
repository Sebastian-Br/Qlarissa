using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qlarissa.Chart.ExcludedTimePeriods
{
    public class ExcludedTimePeriod
    {
        [JsonConstructor]
        public ExcludedTimePeriod(DateOnly? startDate, DateOnly? endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
            if(StartDate == null && EndDate == null)
            {
                throw new Exception("The Start- and EndDate for an ExcludedTimePeriod can not both be null");
            }
        }

        /// <summary>
        /// If the StartDate is null, excludes all data points up to and including the EndDate
        /// </summary>
        public DateOnly? StartDate { get; set; }

        /// <summary>
        /// If the EndDate is null, excludes all data points back to and including the StartDate
        /// </summary>
        public DateOnly? EndDate { get; set; }
    }
}