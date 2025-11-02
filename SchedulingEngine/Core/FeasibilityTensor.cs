using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using AutoScheduling3.Models;

namespace AutoScheduling3.SchedulingEngine.Core
{
    /// <summary>
    /// 可行性张量：三维布尔张量 [哨位, 时段, 人员] - 对应需求7.1-7.5
    /// 用于快速判断某个分配方案是否可行，使用二进制存储和逐位与运算优化性能
    /// </summary>
    public class FeasibilityTensor
    {
        private readonly bool[,,] _tensor;
        private readonly int _positionCount;
        private readonly int _periodCount;
        private readonly int _personCount;

        // 二进制存储优化 - 对应需求7.2
        private readonly ulong[,,] _binaryTensor;
        private readonly int _binarySlices;

        // MathNet.Numerics 矩阵用于加速计算 - 对应需求7.3
        private Matrix<double>? _constraintMatrix;
        private readonly bool _useOptimizedOperations;

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
        /// 构造函数：初始化三维张量 - 对应需求7.1, 7.2
        /// </summary>
        /// <param name="positionCount">哨位数量</param>
        /// <param name="periodCount">时段数量（固定为12）</param>
        /// <param name="personCount">人员数量</param>
        /// <param name="useOptimizedOperations">是否使用优化操作（默认true）</param>
        public FeasibilityTensor(int positionCount, int periodCount, int personCount, bool useOptimizedOperations = true)
        {
            _positionCount = positionCount;
            _periodCount = periodCount;
            _personCount = personCount;
            _useOptimizedOperations = useOptimizedOperations;

            // 初始化三维张量，所有元素初始值为 false（不可行）
            // 在新数据模型中，默认所有人员都不可行，只有在哨位可用人员列表中的才可行
            _tensor = new bool[positionCount, periodCount, personCount];
            for (int x = 0; x < positionCount; x++)
            {
                for (int y = 0; y < periodCount; y++)
                {
                    for (int z = 0; z < personCount; z++)
                    {
                        _tensor[x, y, z] = false;
                    }
                }
            }

            // 初始化二进制存储 - 对应需求7.2
            _binarySlices = (personCount + 63) / 64; // 每个ulong存储64个人员的状态
            _binaryTensor = new ulong[positionCount, periodCount, _binarySlices];
            InitializeBinaryTensorEmpty();

            // 初始化MathNet矩阵 - 对应需求7.3
            if (_useOptimizedOperations && positionCount * periodCount <= 10000) // 避免过大矩阵
            {
                _constraintMatrix = DenseMatrix.Create(positionCount * periodCount, personCount, 0.0);
            }
        }

        /// <summary>
        /// 初始化二进制张量（全可行） - 对应需求7.2
        /// </summary>
        private void InitializeBinaryTensor()
        {
            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    for (int slice = 0; slice < _binarySlices; slice++)
                    {
                        _binaryTensor[x, y, slice] = ulong.MaxValue; // 所有位初始为1（可行）
                    }
                    
                    // 处理最后一个slice的多余位
                    int remainingBits = _personCount % 64;
                    if (remainingBits > 0 && _binarySlices > 0)
                    {
                        ulong mask = (1UL << remainingBits) - 1;
                        _binaryTensor[x, y, _binarySlices - 1] = mask;
                    }
                }
            }
        }

        /// <summary>
        /// 初始化二进制张量（全不可行） - 对应需求4.3
        /// 用于新数据模型，默认所有人员都不可行
        /// </summary>
        private void InitializeBinaryTensorEmpty()
        {
            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    for (int slice = 0; slice < _binarySlices; slice++)
                    {
                        _binaryTensor[x, y, slice] = 0UL; // 所有位初始为0（不可行）
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
        /// 将指定人员在所有哨位和时段设为不可行 - 优化版本
        /// </summary>
        public void SetPersonInfeasible(int personIdx)
        {
            if (_useOptimizedOperations)
            {
                SetPersonInfeasibleOptimized(personIdx);
            }
            else
            {
                SetPersonInfeasibleBasic(personIdx);
            }
        }

        /// <summary>
        /// 基础版本：将指定人员设为不可行
        /// </summary>
        private void SetPersonInfeasibleBasic(int personIdx)
        {
            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    _tensor[x, y, personIdx] = false;
                }
            }
            SyncBinaryTensorFromBool();
        }

        /// <summary>
        /// 优化版本：使用二进制操作将指定人员设为不可行
        /// </summary>
        private void SetPersonInfeasibleOptimized(int personIdx)
        {
            int slice = personIdx / 64;
            int bitPosition = personIdx % 64;
            ulong mask = ~(1UL << bitPosition); // 创建掩码，对应位为0，其他位为1

            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    _binaryTensor[x, y, slice] &= mask;
                }
            }
            SyncBoolTensorFromBinary();
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
        /// 统计指定哨位和时段的可行人员数 - 优化版本
        /// </summary>
        public int CountFeasiblePersons(int positionIdx, int periodIdx)
        {
            if (_useOptimizedOperations)
            {
                return CountFeasiblePersonsOptimized(positionIdx, periodIdx);
            }
            else
            {
                return CountFeasiblePersonsBasic(positionIdx, periodIdx);
            }
        }

        /// <summary>
        /// 基础版本：统计可行人员数
        /// </summary>
        private int CountFeasiblePersonsBasic(int positionIdx, int periodIdx)
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
        /// 优化版本：使用位计数统计可行人员数
        /// </summary>
        private int CountFeasiblePersonsOptimized(int positionIdx, int periodIdx)
        {
            int count = 0;
            for (int slice = 0; slice < _binarySlices; slice++)
            {
                ulong value = _binaryTensor[positionIdx, periodIdx, slice];
                count += System.Numerics.BitOperations.PopCount(value);
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
        /// 应用约束张量（与运算） - 对应需求7.4
        /// 使用逐位与运算优化性能
        /// </summary>
        public void ApplyConstraint(bool[,,] constraintTensor)
        {
            if (_useOptimizedOperations)
            {
                ApplyConstraintOptimized(constraintTensor);
            }
            else
            {
                ApplyConstraintBasic(constraintTensor);
            }
        }

        /// <summary>
        /// 基础约束应用方法
        /// </summary>
        private void ApplyConstraintBasic(bool[,,] constraintTensor)
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
            SyncBinaryTensorFromBool();
        }

        /// <summary>
        /// 优化的约束应用方法 - 对应需求7.4
        /// 使用逐位与运算处理约束条件
        /// </summary>
        private void ApplyConstraintOptimized(bool[,,] constraintTensor)
        {
            // 将约束张量转换为二进制格式并应用逐位与运算
            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    for (int slice = 0; slice < _binarySlices; slice++)
                    {
                        ulong constraintMask = 0;
                        int startPerson = slice * 64;
                        int endPerson = Math.Min(startPerson + 64, _personCount);

                        // 构建约束掩码
                        for (int z = startPerson; z < endPerson; z++)
                        {
                            if (constraintTensor[x, y, z])
                            {
                                constraintMask |= (1UL << (z - startPerson));
                            }
                        }

                        // 应用逐位与运算
                        _binaryTensor[x, y, slice] &= constraintMask;
                    }
                }
            }
            SyncBoolTensorFromBinary();
        }

        /// <summary>
        /// 从布尔张量同步到二进制张量
        /// </summary>
        private void SyncBinaryTensorFromBool()
        {
            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    for (int slice = 0; slice < _binarySlices; slice++)
                    {
                        ulong value = 0;
                        int startPerson = slice * 64;
                        int endPerson = Math.Min(startPerson + 64, _personCount);

                        for (int z = startPerson; z < endPerson; z++)
                        {
                            if (_tensor[x, y, z])
                            {
                                value |= (1UL << (z - startPerson));
                            }
                        }
                        _binaryTensor[x, y, slice] = value;
                    }
                }
            }
        }

        /// <summary>
        /// 从二进制张量同步到布尔张量
        /// </summary>
        private void SyncBoolTensorFromBinary()
        {
            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    for (int slice = 0; slice < _binarySlices; slice++)
                    {
                        ulong value = _binaryTensor[x, y, slice];
                        int startPerson = slice * 64;
                        int endPerson = Math.Min(startPerson + 64, _personCount);

                        for (int z = startPerson; z < endPerson; z++)
                        {
                            _tensor[x, y, z] = (value & (1UL << (z - startPerson))) != 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 批量应用约束 - 对应需求7.4
        /// 使用MathNet.Numerics优化批量约束处理
        /// </summary>
        /// <param name="constraints">约束列表</param>
        public void ApplyBatchConstraints(List<(int positionIdx, int periodIdx, int[] infeasiblePersons)> constraints)
        {
            if (!_useOptimizedOperations || _constraintMatrix == null)
            {
                ApplyBatchConstraintsBasic(constraints);
                return;
            }

            // 使用矩阵运算批量处理约束
            foreach (var (positionIdx, periodIdx, infeasiblePersons) in constraints)
            {
                int matrixRow = positionIdx * _periodCount + periodIdx;
                foreach (var personIdx in infeasiblePersons)
                {
                    _constraintMatrix[matrixRow, personIdx] = 0.0;
                }
            }

            // 将矩阵结果同步回张量
            SyncTensorFromMatrix();
        }

        /// <summary>
        /// 基础版本的批量约束应用
        /// </summary>
        private void ApplyBatchConstraintsBasic(List<(int positionIdx, int periodIdx, int[] infeasiblePersons)> constraints)
        {
            foreach (var (positionIdx, periodIdx, infeasiblePersons) in constraints)
            {
                foreach (var personIdx in infeasiblePersons)
                {
                    _tensor[positionIdx, periodIdx, personIdx] = false;
                }
            }
            SyncBinaryTensorFromBool();
        }

        /// <summary>
        /// 从矩阵同步到张量
        /// </summary>
        private void SyncTensorFromMatrix()
        {
            if (_constraintMatrix == null) return;

            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    int matrixRow = x * _periodCount + y;
                    for (int z = 0; z < _personCount; z++)
                    {
                        _tensor[x, y, z] = _constraintMatrix[matrixRow, z] > 0.5;
                    }
                }
            }
            SyncBinaryTensorFromBool();
        }

        /// <summary>
        /// 从张量同步到矩阵
        /// </summary>
        private void SyncMatrixFromTensor()
        {
            if (_constraintMatrix == null) return;

            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    int matrixRow = x * _periodCount + y;
                    for (int z = 0; z < _personCount; z++)
                    {
                        _constraintMatrix[matrixRow, z] = _tensor[x, y, z] ? 1.0 : 0.0;
                    }
                }
            }
        }

        /// <summary>
        /// 获取张量的内存使用统计
        /// </summary>
        public TensorMemoryStats GetMemoryStats()
        {
            long boolTensorSize = (long)_positionCount * _periodCount * _personCount * sizeof(bool);
            long binaryTensorSize = (long)_positionCount * _periodCount * _binarySlices * sizeof(ulong);
            long matrixSize = _constraintMatrix != null ? 
                (long)_constraintMatrix.RowCount * _constraintMatrix.ColumnCount * sizeof(double) : 0;

            return new TensorMemoryStats
            {
                BoolTensorBytes = boolTensorSize,
                BinaryTensorBytes = binaryTensorSize,
                MatrixBytes = matrixSize,
                TotalBytes = boolTensorSize + binaryTensorSize + matrixSize,
                CompressionRatio = binaryTensorSize > 0 ? (double)boolTensorSize / binaryTensorSize : 1.0
            };
        }

        /// <summary>
        /// 验证张量一致性（调试用）
        /// </summary>
        public bool ValidateConsistency()
        {
            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    for (int z = 0; z < _personCount; z++)
                    {
                        int slice = z / 64;
                        int bitPosition = z % 64;
                        bool binaryValue = (_binaryTensor[x, y, slice] & (1UL << bitPosition)) != 0;
                        
                        if (_tensor[x, y, z] != binaryValue)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 克隆张量 - 优化版本
        /// </summary>
        public FeasibilityTensor Clone()
        {
            var cloned = new FeasibilityTensor(_positionCount, _periodCount, _personCount, _useOptimizedOperations);
            
            // 复制布尔张量
            Array.Copy(_tensor, cloned._tensor, _tensor.Length);
            
            // 复制二进制张量
            Array.Copy(_binaryTensor, cloned._binaryTensor, _binaryTensor.Length);
            
            // 复制矩阵（如果存在）
            if (_constraintMatrix != null && cloned._constraintMatrix != null)
            {
                _constraintMatrix.CopyTo(cloned._constraintMatrix);
            }
            
            return cloned;
        }

        /// <summary>
        /// 使用哨位可用人员列表初始化张量 - 对应需求4.3
        /// </summary>
        /// <param name="positions">哨位列表</param>
        /// <param name="personIdToIdx">人员ID到索引的映射</param>
        public void InitializeWithAvailablePersonnel(List<PositionLocation> positions, Dictionary<int, int> personIdToIdx)
        {
            // 首先重置为全不可行状态
            ResetToEmpty();

            // 为每个哨位设置其可用人员为可行
            for (int posIdx = 0; posIdx < Math.Min(positions.Count, _positionCount); posIdx++)
            {
                var position = positions[posIdx];
                
                foreach (var personnelId in position.AvailablePersonnelIds)
                {
                    if (personIdToIdx.TryGetValue(personnelId, out int personIdx) && 
                        personIdx >= 0 && personIdx < _personCount)
                    {
                        // 将该人员在此哨位的所有时段设为可行
                        for (int periodIdx = 0; periodIdx < _periodCount; periodIdx++)
                        {
                            _tensor[posIdx, periodIdx, personIdx] = true;
                        }
                    }
                }
            }

            // 同步到二进制张量和矩阵
            SyncBinaryTensorFromBool();
            if (_constraintMatrix != null)
            {
                SyncMatrixFromTensor();
            }
        }

        /// <summary>
        /// 重置张量为全不可行状态 - 对应需求4.3
        /// </summary>
        public void ResetToEmpty()
        {
            // 重置布尔张量为全不可行
            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    for (int z = 0; z < _personCount; z++)
                    {
                        _tensor[x, y, z] = false;
                    }
                }
            }

            // 重置二进制张量
            InitializeBinaryTensorEmpty();

            // 重置矩阵
            if (_constraintMatrix != null)
            {
                _constraintMatrix.Clear();
                _constraintMatrix = DenseMatrix.Create(_positionCount * _periodCount, _personCount, 0.0);
            }
        }

        /// <summary>
        /// 重置张量为全可行状态
        /// </summary>
        public void Reset()
        {
            // 重置布尔张量
            for (int x = 0; x < _positionCount; x++)
            {
                for (int y = 0; y < _periodCount; y++)
                {
                    for (int z = 0; z < _personCount; z++)
                    {
                        _tensor[x, y, z] = true;
                    }
                }
            }

            // 重置二进制张量
            InitializeBinaryTensor();

            // 重置矩阵
            if (_constraintMatrix != null)
            {
                _constraintMatrix.Clear();
                _constraintMatrix = DenseMatrix.Create(_positionCount * _periodCount, _personCount, 1.0);
            }
        }

        public override string ToString()
        {
            var stats = GetMemoryStats();
            return $"FeasibilityTensor[{_positionCount}×{_periodCount}×{_personCount}] " +
                   $"Memory: {stats.TotalBytes / 1024.0:F1}KB " +
                   $"Compression: {stats.CompressionRatio:F2}x " +
                   $"Optimized: {_useOptimizedOperations}";
        }
    }

    /// <summary>
    /// 张量内存使用统计
    /// </summary>
    public class TensorMemoryStats
    {
        public long BoolTensorBytes { get; set; }
        public long BinaryTensorBytes { get; set; }
        public long MatrixBytes { get; set; }
        public long TotalBytes { get; set; }
        public double CompressionRatio { get; set; }
    }
}
