using System;

namespace ResourcePlanPro.API.Utilities
{
    public static class DateTimeHelper
    {
        /// <summary>
        /// Gets the start of the week (Monday) for a given date
        /// </summary>
        public static DateTime GetWeekStartDate(DateTime date)
        {
            var diff = date.DayOfWeek - DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            return date.AddDays(-diff).Date;
        }

        /// <summary>
        /// Gets the end of the week (Sunday) for a given date
        /// </summary>
        public static DateTime GetWeekEndDate(DateTime date)
        {
            return GetWeekStartDate(date).AddDays(6);
        }

        /// <summary>
        /// Calculates the number of weeks between two dates
        /// </summary>
        public static int GetWeekCount(DateTime startDate, DateTime endDate)
        {
            var start = GetWeekStartDate(startDate);
            var end = GetWeekStartDate(endDate);
            return (int)Math.Ceiling((end - start).TotalDays / 7) + 1;
        }

        /// <summary>
        /// Generates a list of week start dates between two dates
        /// </summary>
        public static List<DateTime> GetWeeksBetween(DateTime startDate, DateTime endDate)
        {
            var weeks = new List<DateTime>();
            var current = GetWeekStartDate(startDate);
            var end = GetWeekStartDate(endDate);

            while (current <= end)
            {
                weeks.Add(current);
                current = current.AddDays(7);
            }

            return weeks;
        }
    }

    public static class ValidationHelper
    {
        /// <summary>
        /// Validates that end date is after start date
        /// </summary>
        public static bool ValidateDateRange(DateTime startDate, DateTime endDate, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (endDate <= startDate)
            {
                errorMessage = "End date must be after start date";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates hours are within reasonable range
        /// </summary>
        public static bool ValidateHours(decimal hours, decimal maxHours = 168, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (hours < 0)
            {
                errorMessage = "Hours cannot be negative";
                return false;
            }

            if (hours > maxHours)
            {
                errorMessage = $"Hours cannot exceed {maxHours}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        public static bool ValidateEmail(string email, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                errorMessage = "Email is required";
                return false;
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email)
                {
                    errorMessage = "Invalid email format";
                    return false;
                }
            }
            catch (FormatException)
            {
                errorMessage = "Invalid email format";
                return false;
            }

            return true;
        }
    }

    public static class CalculationHelper
    {
        /// <summary>
        /// Calculates utilization percentage
        /// </summary>
        public static decimal CalculateUtilization(decimal usedHours, decimal totalHours)
        {
            if (totalHours == 0) return 0;
            return Math.Round((usedHours / totalHours) * 100, 2);
        }

        /// <summary>
        /// Calculates staffing percentage for a requirement
        /// </summary>
        public static decimal CalculateStaffingPercentage(decimal assignedHours, decimal requiredHours)
        {
            if (requiredHours == 0) return 100;
            return Math.Round((assignedHours / requiredHours) * 100, 2);
        }

        /// <summary>
        /// Determines staffing status based on percentage
        /// </summary>
        public static string GetStaffingStatus(decimal staffingPercentage)
        {
            if (staffingPercentage < 85) return "Understaffed";
            if (staffingPercentage > 110) return "Overstaffed";
            return "Adequate";
        }

        /// <summary>
        /// Determines load level based on utilization
        /// </summary>
        public static string GetLoadLevel(decimal utilization)
        {
            if (utilization < 60) return "Light";
            if (utilization < 85) return "Medium";
            return "Heavy";
        }
    }

    public static class StringHelper
    {
        /// <summary>
        /// Truncates a string to specified length with ellipsis
        /// </summary>
        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }

        /// <summary>
        /// Converts string to title case
        /// </summary>
        public static string ToTitleCase(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
        }

        /// <summary>
        /// Generates initials from a name
        /// </summary>
        public static string GetInitials(string firstName, string lastName)
        {
            var first = string.IsNullOrEmpty(firstName) ? "" : firstName[0].ToString().ToUpper();
            var last = string.IsNullOrEmpty(lastName) ? "" : lastName[0].ToString().ToUpper();
            return first + last;
        }
    }
}
