using System;

namespace AutoScheduling3.SchedulingEngine.Config
{
    /// <summary>
    /// 全局调度配置类
    /// 对应需求6, 9, 11
    /// </summary>
    public class GlobalSchedulingConfig
    {
        private int _maxDaysForGlobalMode = 30;
        private long _memoryThresholdMB = 500;
        private int _segmentSizeDays = 7;

        /// <summary>
        /// 是否启用全局调度模式
        /// 对应需求9.1
        /// 默认值: true
        /// </summary>
        public bool EnableGlobalScheduling { get; set; } = true;

        /// <summary>
        /// 全局调度的最大天数阈值
        /// 超过此值自动降级到按天模式
        /// 对应需求11.1
        /// 默认值: 30天
        /// </summary>
        public int MaxDaysForGlobalMode
        {
            get => _maxDaysForGlobalMode;
            set
            {
                if (value <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[警告] MaxDaysForGlobalMode 必须大于0，使用默认值30");
                    _maxDaysForGlobalMode = 30;
                }
                else
                {
                    _maxDaysForGlobalMode = value;
                }
            }
        }

        /// <summary>
        /// 内存占用阈值(MB)
        /// 超过此值自动降级到按天模式
        /// 对应需求11.3
        /// 默认值: 500MB
        /// </summary>
        public long MemoryThresholdMB
        {
            get => _memoryThresholdMB;
            set
            {
                if (value <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[警告] MemoryThresholdMB 必须大于0，使用默认值500");
                    _memoryThresholdMB = 500;
                }
                else
                {
                    _memoryThresholdMB = value;
                }
            }
        }

        /// <summary>
        /// 是否启用分段处理
        /// 对应需求11.2
        /// 默认值: false
        /// </summary>
        public bool EnableSegmentedProcessing { get; set; } = false;

        /// <summary>
        /// 分段大小(天数)
        /// 对应需求11.2
        /// 默认值: 7天
        /// </summary>
        public int SegmentSizeDays
        {
            get => _segmentSizeDays;
            set
            {
                if (value <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[警告] SegmentSizeDays 必须大于0，使用默认值7");
                    _segmentSizeDays = 7;
                }
                else
                {
                    _segmentSizeDays = value;
                }
            }
        }

        /// <summary>
        /// 验证配置参数的有效性
        /// 对应需求9.4
        /// </summary>
        /// <returns>配置是否有效</returns>
        public bool Validate()
        {
            bool isValid = true;

            if (MaxDaysForGlobalMode <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[配置错误] MaxDaysForGlobalMode 必须大于0");
                isValid = false;
            }

            if (MemoryThresholdMB <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[配置错误] MemoryThresholdMB 必须大于0");
                isValid = false;
            }

            if (EnableSegmentedProcessing && SegmentSizeDays <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[配置错误] SegmentSizeDays 必须大于0");
                isValid = false;
            }

            if (EnableSegmentedProcessing && SegmentSizeDays > MaxDaysForGlobalMode)
            {
                System.Diagnostics.Debug.WriteLine($"[配置警告] SegmentSizeDays ({SegmentSizeDays}) 大于 MaxDaysForGlobalMode ({MaxDaysForGlobalMode})");
            }

            return isValid;
        }

        /// <summary>
        /// 从现有配置迁移到全局调度配置
        /// 对应需求9.1-9.5
        /// </summary>
        /// <returns>新的全局调度配置实例</returns>
        public static GlobalSchedulingConfig CreateDefault()
        {
            return new GlobalSchedulingConfig
            {
                EnableGlobalScheduling = true,
                MaxDaysForGlobalMode = 30,
                MemoryThresholdMB = 500,
                EnableSegmentedProcessing = false,
                SegmentSizeDays = 7
            };
        }

        /// <summary>
        /// 获取配置的描述信息（用于调试和日志）
        /// </summary>
        public override string ToString()
        {
            return $"GlobalSchedulingConfig [Enabled={EnableGlobalScheduling}, " +
                   $"MaxDays={MaxDaysForGlobalMode}, " +
                   $"MemoryThreshold={MemoryThresholdMB}MB, " +
                   $"SegmentedProcessing={EnableSegmentedProcessing}, " +
                   $"SegmentSize={SegmentSizeDays}天]";
        }
    }
}
