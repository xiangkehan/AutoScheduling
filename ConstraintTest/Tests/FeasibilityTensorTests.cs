using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.Tests
{
    /// <summary>
    /// 可行性张量单元测试 (MSTest)
    /// </summary>
    [TestClass]
    public class FeasibilityTensorTests
    {
        [TestMethod]
        public void Constructor_InitializesAllElementsToTrue()
        {
            // Arrange & Act
            var tensor = new FeasibilityTensor(3, 12, 5);

            // Assert
            Assert.AreEqual(3, tensor.PositionCount);
            Assert.AreEqual(12, tensor.PeriodCount);
            Assert.AreEqual(5, tensor.PersonCount);

            // 验证所有元素初始化为 true
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 12; y++)
                {
                    for (int z = 0; z < 5; z++)
                    {
                        Assert.IsTrue(tensor[x, y, z], $"位置 [{x},{y},{z}] 应该初始化为 true");
                    }
                }
            }
        }

        [TestMethod]
        public void SetPersonInfeasible_SetsAllSlotsToFalse()
        {
            // Arrange
            var tensor = new FeasibilityTensor(3, 12, 5);
            int personIdx = 2;

            // Act
            tensor.SetPersonInfeasible(personIdx);

            // Assert
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 12; y++)
                {
                    Assert.IsFalse(tensor[x, y, personIdx], 
                        $"人员 {personIdx} 在位置 [{x},{y}] 应该不可行");
                }
            }

            // 其他人员应该仍然可行
            Assert.IsTrue(tensor[0, 0, 0]);
            Assert.IsTrue(tensor[0, 0, 1]);
        }

        [TestMethod]
        public void SetPersonInfeasibleForPosition_OnlyAffectsSpecificPosition()
        {
            // Arrange
            var tensor = new FeasibilityTensor(3, 12, 5);
            int personIdx = 1;
            int positionIdx = 1;

            // Act
            tensor.SetPersonInfeasibleForPosition(personIdx, positionIdx);

            // Assert
            for (int y = 0; y < 12; y++)
            {
                Assert.IsFalse(tensor[positionIdx, y, personIdx], 
                    $"人员 {personIdx} 在哨位 {positionIdx} 的时段 {y} 应该不可行");
            }

            // 其他哨位应该仍然可行
            Assert.IsTrue(tensor[0, 0, personIdx]);
            Assert.IsTrue(tensor[2, 0, personIdx]);
        }

        [TestMethod]
        public void SetPersonInfeasibleForPeriod_OnlyAffectsSpecificPeriod()
        {
            // Arrange
            var tensor = new FeasibilityTensor(3, 12, 5);
            int personIdx = 2;
            int periodIdx = 5;

            // Act
            tensor.SetPersonInfeasibleForPeriod(personIdx, periodIdx);

            // Assert
            for (int x = 0; x < 3; x++)
            {
                Assert.IsFalse(tensor[x, periodIdx, personIdx], 
                    $"人员 {personIdx} 在时段 {periodIdx} 的哨位 {x} 应该不可行");
            }

            // 其他时段应该仍然可行
            Assert.IsTrue(tensor[0, 0, personIdx]);
            Assert.IsTrue(tensor[0, 11, personIdx]);
        }

        [TestMethod]
        public void CountFeasiblePersons_ReturnsCorrectCount()
        {
            // Arrange
            var tensor = new FeasibilityTensor(2, 12, 4);
            tensor.SetPersonInfeasible(0);
            tensor.SetPersonInfeasible(2);

            // Act
            int count = tensor.CountFeasiblePersons(0, 0);

            // Assert
            Assert.AreEqual(2, count); // 只有人员1和3可行
        }

        [TestMethod]
        public void GetFeasiblePersons_ReturnsCorrectIndices()
        {
            // Arrange
            var tensor = new FeasibilityTensor(2, 12, 5);
            tensor.SetPersonInfeasible(1);
            tensor.SetPersonInfeasible(3);

            // Act
            int[] feasible = tensor.GetFeasiblePersons(0, 0);

            // Assert
            Assert.AreEqual(3, feasible.Length);
            CollectionAssert.Contains(feasible, 0);
            CollectionAssert.Contains(feasible, 2);
            CollectionAssert.Contains(feasible, 4);
            CollectionAssert.DoesNotContain(feasible, 1);
            CollectionAssert.DoesNotContain(feasible, 3);
        }

        [TestMethod]
        public void SetOthersInfeasibleForSlot_OnlyAssignedPersonRemainsFeasible()
        {
            // Arrange
            var tensor = new FeasibilityTensor(2, 12, 4);
            int posIdx = 0;
            int periodIdx = 5;
            int assignedPersonIdx = 2;

            // Act
            tensor.SetOthersInfeasibleForSlot(posIdx, periodIdx, assignedPersonIdx);

            // Assert
            Assert.IsTrue(tensor[posIdx, periodIdx, assignedPersonIdx], 
                "已分配人员应该仍然可行");
            
            for (int z = 0; z < 4; z++)
            {
                if (z != assignedPersonIdx)
                {
                    Assert.IsFalse(tensor[posIdx, periodIdx, z], 
                        $"其他人员 {z} 在该位置应该不可行");
                }
            }
        }

        [TestMethod]
        public void SetOtherPositionsInfeasibleForPersonPeriod_OnlyAssignedPositionRemainsFeasible()
        {
            // Arrange
            var tensor = new FeasibilityTensor(3, 12, 4);
            int personIdx = 1;
            int periodIdx = 7;
            int assignedPositionIdx = 1;

            // Act
            tensor.SetOtherPositionsInfeasibleForPersonPeriod(personIdx, periodIdx, assignedPositionIdx);

            // Assert
            Assert.IsTrue(tensor[assignedPositionIdx, periodIdx, personIdx], 
                "已分配哨位应该仍然可行");
            
            for (int x = 0; x < 3; x++)
            {
                if (x != assignedPositionIdx)
                {
                    Assert.IsFalse(tensor[x, periodIdx, personIdx], 
                        $"其他哨位 {x} 在该时段应该不可行");
                }
            }
        }

        [TestMethod]
        public void Clone_CreatesIndependentCopy()
        {
            // Arrange
            var original = new FeasibilityTensor(2, 12, 3);
            original[0, 0, 0] = false;
            original[1, 5, 2] = false;

            // Act
            var cloned = original.Clone();
            cloned[0, 0, 1] = false; // 修改克隆

            // Assert
            Assert.IsFalse(cloned[0, 0, 0]); // 克隆保留了原始修改
            Assert.IsFalse(cloned[1, 5, 2]);
            Assert.IsFalse(cloned[0, 0, 1]); // 克隆的修改生效

            Assert.IsTrue(original[0, 0, 1]); // 原始张量未受影响
        }

        [TestMethod]
        public void GetFeasiblePersons_EmptyWhenAllInfeasible()
        {
            // Arrange
            var tensor = new FeasibilityTensor(2, 12, 3);
            tensor.SetPersonInfeasible(0);
            tensor.SetPersonInfeasible(1);
            tensor.SetPersonInfeasible(2);

            // Act
            int[] feasible = tensor.GetFeasiblePersons(0, 0);

            // Assert
            Assert.AreEqual(0, feasible.Length, "所有人员不可行时应返回空数组");
        }
    }
}
