using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.Services.ImportExport.Comparison
{
    /// <summary>
    /// 数据比较器基类：提供通用的比较方法
    /// 用于比较导入数据与现有数据，跳过不必要的更新操作
    /// </summary>
    public abstract class DataComparerBase
    {
        /// <summary>
        /// 比较两个整数数组是否相等（忽略顺序）
        /// </summary>
        protected bool AreArraysEqual(int[]? array1, int[]? array2)
        {
            if (array1 == null && array2 == null) return true;
            if (array1 == null || array2 == null) return false;
            if (array1.Length != array2.Length) return false;

            return array1.OrderBy(x => x).SequenceEqual(array2.OrderBy(x => x));
        }

        /// <summary>
        /// 比较两个整数列表是否相等（忽略顺序）
        /// </summary>
        protected bool AreListsEqual(List<int>? list1, List<int>? list2)
        {
            if (list1 == null && list2 == null) return true;
            if (list1 == null || list2 == null) return false;
            if (list1.Count != list2.Count) return false;

            return list1.OrderBy(x => x).SequenceEqual(list2.OrderBy(x => x));
        }

        /// <summary>
        /// 比较两个字符串是否相等（处理 null 值）
        /// </summary>
        protected bool AreStringsEqual(string? str1, string? str2)
        {
            // 将 null 和空字符串视为相等
            var s1 = str1 ?? string.Empty;
            var s2 = str2 ?? string.Empty;
            return string.Equals(s1, s2, StringComparison.Ordinal);
        }

        /// <summary>
        /// 比较两个可空整数是否相等
        /// </summary>
        protected bool AreNullableIntsEqual(int? int1, int? int2)
        {
            if (int1 == null && int2 == null) return true;
            if (int1 == null || int2 == null) return false;
            return int1.Value == int2.Value;
        }

        /// <summary>
        /// 比较两个日期时间是否相等（仅比较日期部分）
        /// </summary>
        protected bool AreDatesEqual(DateTime date1, DateTime date2)
        {
            return date1.Date == date2.Date;
        }

        /// <summary>
        /// 比较两个日期时间列表是否相等（仅比较日期部分，忽略顺序）
        /// </summary>
        protected bool AreDateListsEqual(List<DateTime>? list1, List<DateTime>? list2)
        {
            if (list1 == null && list2 == null) return true;
            if (list1 == null || list2 == null) return false;
            if (list1.Count != list2.Count) return false;

            var dates1 = list1.Select(d => d.Date).OrderBy(d => d).ToList();
            var dates2 = list2.Select(d => d.Date).OrderBy(d => d).ToList();

            return dates1.SequenceEqual(dates2);
        }

        /// <summary>
        /// 比较两个 DayOfWeek 列表是否相等（忽略顺序）
        /// </summary>
        protected bool AreDayOfWeekListsEqual(List<DayOfWeek>? list1, List<DayOfWeek>? list2)
        {
            if (list1 == null && list2 == null) return true;
            if (list1 == null || list2 == null) return false;
            if (list1.Count != list2.Count) return false;

            return list1.OrderBy(x => x).SequenceEqual(list2.OrderBy(x => x));
        }
    }
}
