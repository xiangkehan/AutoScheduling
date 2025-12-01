# PersonScoreState 动态计算测试说明

## 改进概述

将 PersonScoreState 从"增量更新"模式改为"动态查询"模式，解决了 MRV 启发式非时间顺序分配导致的缓存不准确问题。

## 核心改动

### 1. 数据结构变化

**旧版本**：
```csharp
public int RecentShiftInterval { get; set; }
public int RecentHolidayInterval { get; set; }
public int[] PeriodIntervals { get; set; }
```

**新版本**：
```csharp
// 只保留人员ID，所有间隔都动态计算
public int PersonalId { get; set; }
```

### 2. 索引结构（SchedulingContext）

```csharp
// 人员分配索引：personId -> 已分配班次的时间戳集合（有序）
public Dictionary<int, SortedSet<int>> PersonAssignmentTimestamps { get; set; }

// 时间戳到班次详情的映射
public Dictionary<int, Dictionary<int, (DateTime date, int period, int positionIdx)>> PersonAssignmentDetails { get; set; }
```

### 3. 动态计算方法

- `CalculateRecentShiftInterval()` - 计算到最近班次的间隔
- `CalculatePeriodInterval()` - 计算到最近同时段班次的间隔
- `CalculateHolidayInterval()` - 计算到最近休息日班次的间隔

## 测试场景

### 场景 1：非时间顺序分配

```
排班顺序（MRV）：
1. 2024-01-10 时段 5 -> 人员 A
2. 2024-01-08 时段 3 -> 人员 A  (回到过去)
3. 2024-01-12 时段 8 -> 人员 A

评分时（2024-01-15 时段 2）：
- 应该找到最近的班次：2024-01-12 时段 8
- 间隔 = (2024-01-15 时段 2) - (2024-01-12 时段 8)
       = (4 * 12 + 2) - (2 * 12 + 8)
       = 50 - 32 = 18 个时段
```

### 场景 2：历史数据整合

```
历史排班：2024-01-01 到 2024-01-09
本次排班：2024-01-10 开始

人员 A 的历史班次：
- 2024-01-05 时段 3 -> 时间戳 = -60
- 2024-01-09 时段 11 -> 时间戳 = -1

本次排班第一个班次：
- 2024-01-10 时段 5 -> 时间戳 = 5

评分时（2024-01-10 时段 0）：
- 最近班次：2024-01-09 时段 11（时间戳 -1）
- 间隔 = 0 - (-1) = 1 个时段
```

### 场景 3：时段平衡

```
人员 A 的历史班次：
- 2024-01-05 时段 3
- 2024-01-08 时段 3
- 2024-01-12 时段 5

评分时（2024-01-15 时段 3）：
- 查找时段 3 的最近班次：2024-01-08 时段 3
- 间隔 = (2024-01-15 时段 3) - (2024-01-08 时段 3)
       = (5 * 12 + 3) - (-2 * 12 + 3)
       = 63 - (-21) = 84 个时段
```

## 性能优化

使用 `SortedSet<int>` 存储时间戳：
- 插入：O(log n)
- 查询最近班次：O(log n)
- 空间复杂度：O(n)，n 为已分配班次数

## 验证方法

1. 运行排班算法
2. 在评分时打印调试信息：
   ```csharp
   var interval = scoreState.CalculateRecentShiftInterval(date, period, context);
   Debug.WriteLine($"人员 {personId} 在 {date} 时段 {period} 的休息间隔: {interval}");
   ```
3. 检查间隔是否正确反映最近的已分配班次

## 预期结果

- ✅ 无论分配顺序如何，间隔计算始终正确
- ✅ 历史数据正确整合
- ✅ 时段平衡和休息日平衡计算准确
- ✅ 性能可接受（O(log n) 查询）
