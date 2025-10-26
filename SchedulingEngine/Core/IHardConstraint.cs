using System;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Constraints
{
    /// <summary>
    /// 硬约束处理器接口：所有硬约束必须实现此接口
    /// </summary>
    public interface IHardConstraint
    {
        /// <summary>
        /// 约束名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 约束描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 应用约束到可行性张量
        /// </summary>
        /// <param name="tensor">可行性张量</param>
        /// <param name="context">调度上下文</param>
        void Apply(FeasibilityTensor tensor, SchedulingContext context);

        /// <summary>
        /// 在分配后更新约束
        /// </summary>
        /// <param name="tensor">可行性张量</param>
        /// <param name="context">调度上下文</param>
        /// <param name="positionIdx">分配的哨位索引</param>
        /// <param name="periodIdx">分配的时段索引</param>
        /// <param name="personIdx">分配的人员索引</param>
        /// <param name="date">分配的日期</param>
        void UpdateAfterAssignment(FeasibilityTensor tensor, SchedulingContext context, 
            int positionIdx, int periodIdx, int personIdx, DateTime date);
    }
}
