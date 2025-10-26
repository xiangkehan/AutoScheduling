using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoScheduling3.Models;

namespace AutoScheduling3.Tests
{
    /// <summary>
    /// 人员评分状态单元测试 (MSTest)
    /// </summary>
    [TestClass]
    public class PersonScoreStateTests
    {
        [TestMethod]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var state = new PersonScoreState(
                personalId: 1,
                recentShiftInterval: 10,
                recentHolidayInterval: 5,
                periodIntervals: new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }
            );

            // Assert
            Assert.AreEqual(1, state.PersonalId);
            Assert.AreEqual(10, state.RecentShiftInterval);
            Assert.AreEqual(5, state.RecentHolidayInterval);
            Assert.AreEqual(12, state.PeriodIntervals.Length);
            Assert.AreEqual(-1, state.LastAssignedPeriod);
            Assert.IsNull(state.LastAssignedDate);
        }

        [TestMethod]
        public void Constructor_WithNullPeriodIntervals_InitializesZeroArray()
        {
            // Arrange & Act
            var state = new PersonScoreState(1);

            // Assert
            Assert.AreEqual(12, state.PeriodIntervals.Length);
            Assert.AreEqual(0, state.PeriodIntervals[0]);
            Assert.AreEqual(0, state.PeriodIntervals[11]);
        }

        [TestMethod]
        public void CalculateScore_WorkDay_ReturnsCorrectScore()
        {
            // Arrange
            var state = new PersonScoreState(
                personalId: 1,
                recentShiftInterval: 10,
                recentHolidayInterval: 5,
                periodIntervals: new int[] { 2, 2, 2, 15, 2, 2, 2, 2, 2, 2, 2, 2 }
            );
            var date = new DateTime(2024, 1, 2); // 工作日（周二）
            int periodIdx = 3;

            // Act
            double score = state.CalculateScore(periodIdx, date, isHoliday: false);

            // Assert
            // 工作日得分 = 充分休息(10) + 时段平衡(15) = 25
            Assert.AreEqual(25.0, score);
        }

        [TestMethod]
        public void CalculateScore_Holiday_IncludesHolidayScore()
        {
            // Arrange
            var state = new PersonScoreState(
                personalId: 1,
                recentShiftInterval: 10,
                recentHolidayInterval: 8,
                periodIntervals: new int[] { 2, 2, 2, 15, 2, 2, 2, 2, 2, 2, 2, 2 }
            );
            var date = new DateTime(2024, 1, 6); // 休息日（周六）
            int periodIdx = 3;

            // Act
            double score = state.CalculateScore(periodIdx, date, isHoliday: true);

            // Assert
            // 休息日得分 = 充分休息(10) + 时段平衡(15) + 休息日平衡(8) = 33
            Assert.AreEqual(33.0, score);
        }

        [TestMethod]
        public void UpdateAfterAssignment_ResetsCorrectIntervals()
        {
            // Arrange
            var state = new PersonScoreState(
                personalId: 1,
                recentShiftInterval: 10,
                recentHolidayInterval: 5,
                periodIntervals: new int[] { 2, 2, 2, 15, 2, 2, 2, 2, 2, 2, 2, 2 }
            );
            var date = new DateTime(2024, 1, 2);
            int periodIdx = 3;

            // Act
            state.UpdateAfterAssignment(periodIdx, date, isHoliday: false);

            // Assert
            Assert.AreEqual(0, state.RecentShiftInterval, "充分休息间隔应重置为0");
            Assert.AreEqual(0, state.PeriodIntervals[periodIdx], "当前时段间隔应重置为0");
            Assert.AreEqual(5, state.RecentHolidayInterval, "工作日不应重置休息日间隔");
            Assert.AreEqual(periodIdx, state.LastAssignedPeriod);
            Assert.AreEqual(date, state.LastAssignedDate);
        }

        [TestMethod]
        public void UpdateAfterAssignment_OnHoliday_ResetsHolidayInterval()
        {
            // Arrange
            var state = new PersonScoreState(
                personalId: 1,
                recentShiftInterval: 10,
                recentHolidayInterval: 8,
                periodIntervals: new int[] { 2, 2, 2, 15, 2, 2, 2, 2, 2, 2, 2, 2 }
            );
            var date = new DateTime(2024, 1, 6); // 休息日
            int periodIdx = 5;

            // Act
            state.UpdateAfterAssignment(periodIdx, date, isHoliday: true);

            // Assert
            Assert.AreEqual(0, state.RecentShiftInterval);
            Assert.AreEqual(0, state.PeriodIntervals[periodIdx]);
            Assert.AreEqual(0, state.RecentHolidayInterval, "休息日应重置休息日间隔");
        }

        [TestMethod]
        public void IncrementIntervals_IncrementsAllIntervals()
        {
            // Arrange
            var state = new PersonScoreState(
                personalId: 1,
                recentShiftInterval: 10,
                recentHolidayInterval: 5,
                periodIntervals: new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }
            );

            // Act
            state.IncrementIntervals();

            // Assert
            Assert.AreEqual(11, state.RecentShiftInterval);
            for (int i = 0; i < 12; i++)
            {
                Assert.AreEqual(i + 2, state.PeriodIntervals[i], $"时段{i}间隔应增加1");
            }
        }

        [TestMethod]
        public void IncrementHolidayInterval_IncrementsOnlyHolidayInterval()
        {
            // Arrange
            var state = new PersonScoreState(
                personalId: 1,
                recentShiftInterval: 10,
                recentHolidayInterval: 5,
                periodIntervals: new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }
            );

            // Act
            state.IncrementHolidayInterval();

            // Assert
            Assert.AreEqual(6, state.RecentHolidayInterval);
            Assert.AreEqual(10, state.RecentShiftInterval, "充分休息间隔不应变化");
            Assert.AreEqual(1, state.PeriodIntervals[0], "时段间隔不应变化");
        }
    }
}
