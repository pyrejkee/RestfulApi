using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Helpers
{
    public static class DateTimeOffsetExtensions
    {
        public static int GetCurrentAge(this DateTimeOffset dateTimeOffset, DateTimeOffset? dateOfDeath)
        {
            var dateToCalculate = DateTime.UtcNow;

            if (dateOfDeath != null)
            {
                dateToCalculate = dateOfDeath.Value.UtcDateTime;
            }

            int age = dateToCalculate.Year - dateTimeOffset.Year;

            if (dateToCalculate < dateTimeOffset.AddYears(age))
            {
                age--;
            }

            return age;
        }
    }
}
