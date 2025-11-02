using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoScheduling3.Controls;
using AutoScheduling3.DTOs;
using System.Collections.ObjectModel;
using System;
using System.Linq;

namespace AutoScheduling3.Tests
{
    [TestClass]
    public class ScheduleGridControlTests
    {
        private ScheduleGridControl _control;
        private ScheduleDto _testSchedule;
        private ObservableCollection<PositionDto> _testPositions;
        private ObservableCollection<PersonnelDto> _testPersonnel;

        [TestInitialize]
        public void Setup()
        {
            _control = new ScheduleGridControl();
            
            // Create test data
            _testPositions = new ObservableCollection<PositionDto>
            {
                new PositionDto { Id = 1, Name = "东门", Location = "东门", RequiredSkillIds = new() { 1, 2 } },
                new PositionDto { Id = 2, Name = "西门", Location = "西门", RequiredSkillIds = new() { 1 } }
            };

            _testPersonnel = new ObservableCollection<PersonnelDto>
            {
                new PersonnelDto { Id = 1, Name = "张三", SkillIds = new() { 1, 2 }, IsAvailable = true },
                new PersonnelDto { Id = 2, Name = "李四", SkillIds = new() { 1 }, IsAvailable = true }
            };

            _testSchedule = new ScheduleDto
            {
                Id = 1,
                Title = "测试排班",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                PersonnelIds = new() { 1, 2 },
                PositionIds = new() { 1, 2 },
                Shifts = new()
                {
                    new ShiftDto
                    {
                        Id = 1,
                        PositionId = 1,
                        PersonnelId = 1,
                        PersonnelName = "张三",
                        StartTime = DateTime.Today,
                        EndTime = DateTime.Today.AddHours(2),
                        PeriodIndex = 0
                    }
                }
            };
        }

        [TestMethod]
        public void TestControlInitialization()
        {
            Assert.IsNotNull(_control);
            Assert.IsFalse(_control.IsReadOnly);
            Assert.IsTrue(_control.ShowConflicts);
            Assert.IsTrue(_control.EnableVirtualization);
        }

        [TestMethod]
        public void TestDataBinding()
        {
            _control.Schedule = _testSchedule;
            _control.Positions = _testPositions;
            _control.Personnels = _testPersonnel;

            Assert.AreEqual(_testSchedule, _control.Schedule);
            Assert.AreEqual(_testPositions, _control.Positions);
            Assert.AreEqual(_testPersonnel, _control.Personnels);
        }

        [TestMethod]
        public void TestCellModelProperties()
        {
            var cellModel = new CellModel
            {
                Date = DateTime.Today,
                PeriodIndex = 0,
                Position = _testPositions.First()
            };

            Assert.IsTrue(cellModel.IsEmpty);
            Assert.IsFalse(cellModel.IsNightShift);
            Assert.AreEqual("00:00-02:00", cellModel.PeriodDisplayText);

            // Test night shift detection
            cellModel.PeriodIndex = 11; // 22:00-00:00
            Assert.IsTrue(cellModel.IsNightShift);
        }

        [TestMethod]
        public void TestConflictDetection()
        {
            var cellModel = new CellModel();
            
            Assert.IsFalse(cellModel.HasConflict);
            Assert.IsNull(cellModel.Conflict);

            cellModel.HasConflict = true;
            cellModel.Conflict = new ConflictDto
            {
                Type = "hard",
                Message = "技能不匹配"
            };

            Assert.IsTrue(cellModel.HasConflict);
            Assert.AreEqual("硬约束", cellModel.ConflictDisplayText);
        }

        [TestMethod]
        public void TestVirtualizationProperties()
        {
            Assert.AreEqual(20, _control.MaxVisibleRows);
            Assert.AreEqual(12, _control.MaxVisibleColumns);

            _control.MaxVisibleRows = 30;
            _control.MaxVisibleColumns = 15;

            Assert.AreEqual(30, _control.MaxVisibleRows);
            Assert.AreEqual(15, _control.MaxVisibleColumns);
        }

        [TestMethod]
        public void TestComputedProperties()
        {
            _control.Schedule = _testSchedule;
            
            // Note: TotalCells will be 0 initially as BuildGrid() hasn't been called
            // In a real scenario, this would be populated after the control is loaded
            Assert.AreEqual(0, _control.TotalCells);
            Assert.AreEqual(0, _control.ConflictCount);
            Assert.IsFalse(_control.HasConflicts);
        }
    }
}