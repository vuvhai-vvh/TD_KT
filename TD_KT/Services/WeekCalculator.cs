using System;
using System.Collections.Generic;
using System.Linq;

namespace TD_KT.Services
{
    /// <summary>
    /// Tuần luôn bắt đầu từ Thứ 2 (Monday) và kết thúc Chủ nhật (Sunday).
    /// Tuần giao tháng/năm: tuần được gán cho tháng/năm nào có >= 4 ngày trong tuần đó.
    /// </summary>
    public static class WeekCalculator
    {
        public sealed class WeekAssignment
        {
            public DateTime WeekStart { get; set; }   // Monday
            public DateTime WeekEnd { get; set; }     // Sunday
            public int AssignedYear { get; set; }
            public int AssignedMonth { get; set; }
            public int WeekNoInAssignedMonth { get; set; } // 1..n within AssignedYear/AssignedMonth
        }

        public sealed class WeekOption
        {
            public int WeekNoInMonth { get; set; } // 1..n for the selected month
            public DateTime StartDate { get; set; } // Monday
            public DateTime EndDate { get; set; }   // Sunday
            public string DisplayText { get; set; } = "";
        }

        /// <summary>
        /// Get Monday of the week containing the date.
        /// </summary>
        public static DateTime GetWeekStartMonday(DateTime date)
        {
            var d = date.Date;
            int dow = (int)d.DayOfWeek; // Sunday=0..Saturday=6
            // Convert to Monday=0..Sunday=6
            int mondayBased = (dow == 0) ? 6 : (dow - 1);
            return d.AddDays(-mondayBased);
        }

        /// <summary>
        /// Determine which month/year the week belongs to (>=4 days rule).
        /// </summary>
        public static (int year, int month) GetAssignedYearMonth(DateTime weekStartMonday)
        {
            var counts = new Dictionary<(int y, int m), int>();
            for (int i = 0; i < 7; i++)
            {
                var d = weekStartMonday.Date.AddDays(i);
                var key = (d.Year, d.Month);
                if (!counts.ContainsKey(key)) counts[key] = 0;
                counts[key]++;
            }

            // Choose the month with max days (always >=4)
            var best = counts.OrderByDescending(kv => kv.Value).First().Key;
            return (best.y, best.m);
        }

        /// <summary>
        /// Build all weeks that are assigned to a given month/year, with display text.
        /// </summary>
        public static List<WeekOption> GetWeeksForMonth(int year, int month)
        {
            var firstDay = new DateTime(year, month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            // Start from Monday of the week containing first day
            var start = GetWeekStartMonday(firstDay);
            // End at Monday of the week containing last day
            var end = GetWeekStartMonday(lastDay);

            var weekStarts = new List<DateTime>();
            for (var ws = start; ws <= end; ws = ws.AddDays(7))
                weekStarts.Add(ws);

            var options = new List<WeekOption>();
            foreach (var ws in weekStarts)
            {
                var assigned = GetAssignedYearMonth(ws);
                if (assigned.year == year && assigned.month == month)
                {
                    var we = ws.AddDays(6);
                    options.Add(new WeekOption
                    {
                        StartDate = ws,
                        EndDate = we
                    });
                }
            }

            // Sort and number
            options = options.OrderBy(o => o.StartDate).ToList();
            for (int i = 0; i < options.Count; i++)
            {
                var o = options[i];
                o.WeekNoInMonth = i + 1;
                o.DisplayText = $"Tuần {o.WeekNoInMonth} ({o.StartDate:dd/MM} - {o.EndDate:dd/MM})";
            }

            return options;
        }

        /// <summary>
        /// Calculate assignment for a datetime (record time): AssignedYear/Month + WeekNoInAssignedMonth.
        /// </summary>
        public static WeekAssignment GetAssignment(DateTime recordDateTime)
        {
            var ws = GetWeekStartMonday(recordDateTime);
            var we = ws.AddDays(6);

            var assigned = GetAssignedYearMonth(ws);
            var weeksOfMonth = GetWeeksForMonth(assigned.year, assigned.month);
            var weekNo = weeksOfMonth.FirstOrDefault(w => w.StartDate == ws)?.WeekNoInMonth ?? 1;

            return new WeekAssignment
            {
                WeekStart = ws,
                WeekEnd = we,
                AssignedYear = assigned.year,
                AssignedMonth = assigned.month,
                WeekNoInAssignedMonth = weekNo
            };
        }
    }
}
