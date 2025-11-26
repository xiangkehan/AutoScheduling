using System.Collections.Generic;

namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 冲突解决选项数据传输对象
    /// </summary>
    public class ConflictResolutionOption
    {
        /// <summary>
        /// 冲突ID
        /// </summary>
        public int ConflictId { get; set; }

        /// <summary>
        /// 方案标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 解决方案类型
        /// </summary>
        public ResolutionType Type { get; set; }

        /// <summary>
        /// 解决方案数据（动态对象）
        /// </summary>
        public object? ResolutionData { get; set; }

        /// <summary>
        /// 是否为推荐方案
        /// </summary>
        public bool IsRecommended { get; set; }

        /// <summary>
        /// 方案描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 优点列表
        /// </summary>
        public List<string> Pros { get; set; } = new();

        /// <summary>
        /// 缺点列表
        /// </summary>
        public List<string> Cons { get; set; } = new();

        /// <summary>
        /// 影响描述
        /// </summary>
        public string? Impact { get; set; }

        /// <summary>
        /// 预期新增冲突数量
        /// </summary>
        public int ExpectedNewConflicts { get; set; }

        /// <summary>
        /// 新的人员ID（用于替换人员操作）
        /// </summary>
        public int? NewPersonnelId { get; set; }

        /// <summary>
        /// 新的人员姓名
        /// </summary>
        public string? NewPersonnelName { get; set; }

        /// <summary>
        /// 新的时段（用于调整班次操作）
        /// </summary>
        public int? NewTimeSlot { get; set; }

        /// <summary>
        /// 是否标记为忽略
        /// </summary>
        public bool MarkAsIgnored { get; set; }
    }
}
