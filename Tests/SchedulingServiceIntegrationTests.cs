using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoScheduling3.Services;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.History;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AutoScheduling3.Tests
{
    /// <summary>
    /// 排班服务集成测试 - 对应需求3.1, 3.2, 4.1, 4.2, 4.3, 4.4
    /// </summary>
    [TestClass]
    public class SchedulingServiceIntegrationTests
    {
        private Mock<IPersonalRepository> _mockPersonalRepo;
        private Mock<IPositionRepository> _mockPositionRepo;
        private Mock<ISkillRepository> _mockSkillRepo;
        private Mock<IConstraintRepository> _mockConstraintRepo;
        private Mock<IHistoryManagement> _mockHistoryMgmt;
        private SchedulingService _schedulingService;

        [TestInitialize]
        public void Setup()
        {
            _mockPersonalRepo = new Mock<IPersonalRepository>();
            _mockPositionRepo = new Mock<IPositionRepository>();
            _mockSkillRepo = new Mock<ISkillRepository>();
            _mockConstraintRepo = new Mock<IConstraintRepository>();
            _mockHistoryMgmt = new Mock<IHistoryManagement>();

            _schedulingService = new SchedulingService(
                _mockPersonalRepo.Object,
                _mockPositionRepo.Object,
                _mockSkillRepo.Object,
                _mockConstraintRepo.Object,
                _mockHistoryMgmt.Object
            );
        }

        /// <summary>
        /// 测试排班服务初始化 - 对应需求3.1, 4.1
        /// </summary>
        [TestMethod]
        public async Task InitializeAsync_ShouldInitializeAllComponents()
        {
            // Arrange
            _mockHistoryMgmt.Setup(h => h.InitAsync()).Returns(Task.CompletedTask);

            // Act
            await _schedulingService.InitializeAsync();

            // Assert
            _mockHistoryMgmt.Verify(h => h.InitAsync(), Times.Once);
        }

        /// <summary>
        /// 测试排班引擎状态获取 - 对应需求3.1, 3.2
        /// </summary>
        [TestMethod]
        public async Task GetSchedulingEngineStatusAsync_ShouldReturnValidStatus()
        {
            // Arrange
            var mockSchedule = new Schedule { Id = 1, Header = "Test Schedule" };
            var mockBuffers = new List<(Schedule Schedule, DateTime CreateTime, int BufferId)>
            {
                (mockSchedule, DateTime.UtcNow, 1)
            };

            _mockHistoryMgmt.Setup(h => h.GetLastConfirmedScheduleAsync())
                .ReturnsAsync(mockSchedule);
            _mockHistoryMgmt.Setup(h => h.GetAllBufferSchedulesAsync())
                .ReturnsAsync(mockBuffers);
            _mockConstraintRepo.Setup(c => c.GetActiveHolidayConfigAsync())
                .ReturnsAsync((HolidayConfig)null);
            _mockConstraintRepo.Setup(c => c.GetAllFixedPositionRulesAsync(true))
                .ReturnsAsync(new List<FixedPositionRule>());

            // Act
            var status = await _schedulingService.GetSchedulingEngineStatusAsync();

            // Assert
            Assert.IsNotNull(status);
            Assert.IsTrue(status.ContainsKey("EngineStatus"));
            Assert.IsTrue(status.ContainsKey("BufferCount"));
            Assert.AreEqual(1, status["BufferCount"]);
        }

        /// <summary>
        /// 测试草稿排班获取 - 对应需求4.1, 4.2
        /// </summary>
        [TestMethod]
        public async Task GetDraftsAsync_ShouldReturnDraftSchedules()
        {
            // Arrange
            var mockSchedule = new Schedule 
            { 
                Id = 1, 
                Header = "Draft Schedule",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                PersonnelIds = new List<int> { 1, 2 },
                PositionIds = new List<int> { 1 },
                Shifts = new List<SingleShift>()
            };
            
            var mockBuffers = new List<(Schedule Schedule, DateTime CreateTime, int BufferId)>
            {
                (mockSchedule, DateTime.UtcNow, 1)
            };

            _mockHistoryMgmt.Setup(h => h.GetAllBufferSchedulesAsync())
                .ReturnsAsync(mockBuffers);

            // Act
            var drafts = await _schedulingService.GetDraftsAsync();

            // Assert
            Assert.IsNotNull(drafts);
            Assert.AreEqual(1, drafts.Count);
            Assert.AreEqual("Draft Schedule", drafts[0].Title);
            Assert.AreEqual(2, drafts[0].PersonnelCount);
            Assert.AreEqual(1, drafts[0].PositionCount);
        }

        /// <summary>
        /// 测试排班确认流程 - 对应需求4.3, 4.4
        /// </summary>
        [TestMethod]
        public async Task ConfirmScheduleAsync_ShouldConfirmValidSchedule()
        {
            // Arrange
            var mockSchedule = new Schedule 
            { 
                Id = 1, 
                Header = "Test Schedule",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today,
                PersonnelIds = new List<int> { 1 },
                PositionIds = new List<int> { 1 },
                Shifts = new List<SingleShift>
                {
                    new SingleShift
                    {
                        Id = 1,
                        PositionId = 1,
                        PersonalId = 1,
                        StartTime = DateTime.Today.AddHours(8),
                        EndTime = DateTime.Today.AddHours(10)
                    }
                }
            };
            
            var mockBuffers = new List<(Schedule Schedule, DateTime CreateTime, int BufferId)>
            {
                (mockSchedule, DateTime.UtcNow, 1)
            };

            _mockHistoryMgmt.Setup(h => h.GetAllBufferSchedulesAsync())
                .ReturnsAsync(mockBuffers);
            _mockHistoryMgmt.Setup(h => h.ConfirmBufferScheduleAsync(1))
                .Returns(Task.CompletedTask);
            _mockConstraintRepo.Setup(c => c.GetActiveHolidayConfigAsync())
                .ReturnsAsync((HolidayConfig)null);
            _mockPersonalRepo.Setup(p => p.GetPersonnelByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new List<Personal>
                {
                    new Personal { Id = 1, Name = "Test Person" }
                });

            // Act & Assert - Should not throw
            await _schedulingService.ConfirmScheduleAsync(1);

            // Verify confirmation was called
            _mockHistoryMgmt.Verify(h => h.ConfirmBufferScheduleAsync(1), Times.Once);
        }

        /// <summary>
        /// 测试排班统计信息获取 - 对应需求4.1, 4.2
        /// </summary>
        [TestMethod]
        public async Task GetScheduleStatisticsAsync_ShouldReturnValidStatistics()
        {
            // Arrange
            var mockSchedule = new Schedule 
            { 
                Id = 1, 
                Header = "Test Schedule",
                Shifts = new List<SingleShift>
                {
                    new SingleShift
                    {
                        PersonalId = 1,
                        StartTime = DateTime.Today.AddHours(8)
                    },
                    new SingleShift
                    {
                        PersonalId = 2,
                        StartTime = DateTime.Today.AddHours(10)
                    }
                }
            };
            
            var mockHistories = new List<(Schedule Schedule, DateTime ConfirmTime)>
            {
                (mockSchedule, DateTime.UtcNow)
            };
            
            var mockBuffers = new List<(Schedule Schedule, DateTime CreateTime, int BufferId)>();

            _mockHistoryMgmt.Setup(h => h.GetAllHistorySchedulesAsync())
                .ReturnsAsync(mockHistories);
            _mockHistoryMgmt.Setup(h => h.GetAllBufferSchedulesAsync())
                .ReturnsAsync(mockBuffers);

            // Act
            var statistics = await _schedulingService.GetScheduleStatisticsAsync();

            // Assert
            Assert.IsNotNull(statistics);
            Assert.AreEqual(1, statistics.TotalSchedules);
            Assert.AreEqual(1, statistics.ConfirmedSchedules);
            Assert.AreEqual(0, statistics.DraftSchedules);
            Assert.IsNotNull(statistics.PersonnelShiftCounts);
            Assert.AreEqual(1, statistics.PersonnelShiftCounts[1]);
            Assert.AreEqual(1, statistics.PersonnelShiftCounts[2]);
        }

        /// <summary>
        /// 测试批量确认排班 - 对应需求4.3, 4.4
        /// </summary>
        [TestMethod]
        public async Task ConfirmMultipleSchedulesAsync_ShouldConfirmAllValidSchedules()
        {
            // Arrange
            var scheduleIds = new List<int> { 1, 2 };
            var mockSchedules = new List<(Schedule Schedule, DateTime CreateTime, int BufferId)>
            {
                (new Schedule 
                { 
                    Id = 1, 
                    Header = "Schedule 1",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today,
                    PersonnelIds = new List<int> { 1 },
                    PositionIds = new List<int> { 1 },
                    Shifts = new List<SingleShift>()
                }, DateTime.UtcNow.AddMinutes(-10), 1),
                (new Schedule 
                { 
                    Id = 2, 
                    Header = "Schedule 2",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today,
                    PersonnelIds = new List<int> { 1 },
                    PositionIds = new List<int> { 1 },
                    Shifts = new List<SingleShift>()
                }, DateTime.UtcNow.AddMinutes(-5), 2)
            };

            _mockHistoryMgmt.Setup(h => h.GetAllBufferSchedulesAsync())
                .ReturnsAsync(mockSchedules);
            _mockHistoryMgmt.Setup(h => h.ConfirmBufferScheduleAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            _mockConstraintRepo.Setup(c => c.GetActiveHolidayConfigAsync())
                .ReturnsAsync((HolidayConfig)null);
            _mockPersonalRepo.Setup(p => p.GetPersonnelByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new List<Personal>
                {
                    new Personal { Id = 1, Name = "Test Person" }
                });

            // Act
            await _schedulingService.ConfirmMultipleSchedulesAsync(scheduleIds);

            // Assert
            _mockHistoryMgmt.Verify(h => h.ConfirmBufferScheduleAsync(1), Times.Once);
            _mockHistoryMgmt.Verify(h => h.ConfirmBufferScheduleAsync(2), Times.Once);
        }

        /// <summary>
        /// 测试过期草稿清理 - 对应需求4.2
        /// </summary>
        [TestMethod]
        public async Task CleanupExpiredDraftsAsync_ShouldRemoveExpiredDrafts()
        {
            // Arrange
            var expiredDate = DateTime.UtcNow.AddDays(-10);
            var recentDate = DateTime.UtcNow.AddDays(-1);
            
            var mockBuffers = new List<(Schedule Schedule, DateTime CreateTime, int BufferId)>
            {
                (new Schedule { Id = 1, Header = "Expired" }, expiredDate, 1),
                (new Schedule { Id = 2, Header = "Recent" }, recentDate, 2)
            };

            _mockHistoryMgmt.Setup(h => h.GetAllBufferSchedulesAsync())
                .ReturnsAsync(mockBuffers);
            _mockHistoryMgmt.Setup(h => h.DeleteBufferScheduleAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            await _schedulingService.CleanupExpiredDraftsAsync(7);

            // Assert
            _mockHistoryMgmt.Verify(h => h.DeleteBufferScheduleAsync(1), Times.Once);
            _mockHistoryMgmt.Verify(h => h.DeleteBufferScheduleAsync(2), Times.Never);
        }
    }
}