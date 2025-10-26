using System;

namespace AutoScheduling3.SchedulingEngine.Core
{
    /// <summary>
    /// 可行性张量：三维布尔张量 [哨位, 时段, 人员]
    /// 用于快速判断某个分配方案是否可行
    /// </summary>
    public class FeasibilityTensor
    {
        private readonly bool[,,] _tensor;
        private readonly int _positionCount;
        private readonly int _periodCount;
        private readonly int _personCount;

        /// <summary>
        /// 哨位数量
        /// </summary>
        public int PositionCount => _positionCount;

        /// <summary>
        /// 时段数量（固定为12）
        /// </summary>
        public int PeriodCount => _periodCount;

        /// <summary>
        /// 人员数量
        /// </summary>
        public int PersonCount => _personCount;

        /// <summary>
        /// 构造函数：初始化三维张量
        /// </summary>
        /// <param name="positionCount">哨位数量</param>
        /// <param name="periodCount">时段数量（固定为12）</param>
        /// <param name="personCount">人员数量</param>
        public FeasibilityTensor(int positionCount, int periodCount, int personCount)
        {
            _positionCount = positionCount;
            _periodCount = periodCount;
            _personCount = personCount;

            // 初始化三维张量，所有元素初始值为 true（可行）
            _tensor = new bool[positionCount, periodCount, personCount];
            for (int x = 0; x < positionCount; x++)
            {
                for (int y = 0; y < periodCount; y++)
                {
                    for (int z = 0; z < personCount; z++)
                    {
                        _tensor[x, y, z] = true;
                    }
                }
            }
        }

        /// <summary>
        /// 获取或设置指定位置的可行性
        /// </summary>
        public bool this[int positionIdx, int periodIdx, int personIdx]
        {
            get => _tensor[positionIdx, periodIdx, personIdx];
            set => _tensor[positionIdx, periodIdx, personIdx] = value;
        }

        /// <summary>
        /// 将指定人员在所有哨位和时段设为不可行
        /// </summary>
        public void SetPersonInfeasible(int personIdx)
        {
            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    _tensor[x, y, personIdx] = false;
                }
            }
        }

        /// <summary>
        /// 将指定人员在特定哨位的所有时段设为不可行
        /// </summary>
        public void SetPersonInfeasibleForPosition(int personIdx, int positionIdx)
        {
            for (int y = 0; y < _periodCount; y++)
            {
                _tensor[positionIdx, y, personIdx] = false;
            }
        }

        /// <summary>
        /// 将指定人员在特定时段的所有哨位设为不可行
        /// </summary>
        public void SetPersonInfeasibleForPeriod(int personIdx, int periodIdx)
        {
            for (int x = 0; x < _positionCount; x++)
            {
                _tensor[x, periodIdx, personIdx] = false;
            }
        }

        /// <summary>
        /// 将指定哨位和时段的所有其他人员设为不可行（单人上哨约束）
        /// </summary>
        public void SetOthersInfeasibleForSlot(int positionIdx, int periodIdx, int assignedPersonIdx)
        {
            for (int z = 0; z < _personCount; z++)
            {
                if (z != assignedPersonIdx)
                {
                    _tensor[positionIdx, periodIdx, z] = false;
                }
            }
        }

        /// <summary>
        /// 将指定人员在特定时段的所有其他哨位设为不可行（一人一哨约束）
        /// </summary>
        public void SetOtherPositionsInfeasibleForPersonPeriod(int personIdx, int periodIdx, int assignedPositionIdx)
        {
            for (int x = 0; x < _positionCount; x++)
            {
                if (x != assignedPositionIdx)
                {
                    _tensor[x, periodIdx, personIdx] = false;
                }
            }
        }

        /// <summary>
        /// 统计指定哨位和时段的可行人员数
        /// </summary>
        public int CountFeasiblePersons(int positionIdx, int periodIdx)
        {
            int count = 0;
            for (int z = 0; z < _personCount; z++)
            {
                if (_tensor[positionIdx, periodIdx, z])
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 获取指定哨位和时段的所有可行人员索引
        /// </summary>
        public int[] GetFeasiblePersons(int positionIdx, int periodIdx)
        {
            var feasibleList = new System.Collections.Generic.List<int>();
            for (int z = 0; z < _personCount; z++)
            {
                if (_tensor[positionIdx, periodIdx, z])
                    feasibleList.Add(z);
            }
            return feasibleList.ToArray();
        }

        /// <summary>
        /// 应用约束张量（与运算）
        /// </summary>
        public void ApplyConstraint(bool[,,] constraintTensor)
        {
            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    for (int z = 0; z < _personCount; z++)
                    {
                        _tensor[x, y, z] &= constraintTensor[x, y, z];
                    }
                }
            }
        }

        /// <summary>
        /// 克隆张量
        /// </summary>
        public FeasibilityTensor Clone()
        {
            var cloned = new FeasibilityTensor(_positionCount, _periodCount, _personCount);
            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    for (int z = 0; z < _personCount; z++)
                    {
                        cloned._tensor[x, y, z] = _tensor[x, y, z];
                    }
                }
            }
            return cloned;
        }

        public override string ToString()
        {
            return $"FeasibilityTensor[{_positionCount}×{_periodCount}×{_personCount}]";
        }
    }
}
