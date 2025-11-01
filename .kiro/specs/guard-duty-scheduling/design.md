# 哨位排班系统设计文档

## 概述

哨位排班系统是基于WinUI 3的桌面应用程序，采用MVVM架构模式，实现自动化的人员调度管理。系统通过贪心算法等调度策略，在满足硬约束条件的前提下，优化软约束评分，生成最优的排班方案。

系统核心功能包括：
- 哨位信息管理：支持哨位的创建、配置和管理
- 人员技能管理：管理人员信息、技能关联和可用性状态
- 智能排班生成：基于约束条件和优化算法自动生成排班方案
- 历史管理：维护排班历史记录，支持草稿和确认机制
- 约束处理：严格执行硬约束，优化软约束评分

## 架构设计

### 整体架构

系统采用分层架构，实现前后端逻辑解耦：

```
┌─────────────────────────────────────────┐
│              WinUI 3 前端层              │
├─────────────────────────────────────────┤
│ Views (XAML) │ ViewModels │ Controls    │
├─────────────────────────────────────────┤
│              业务逻辑层                  │
├─────────────────────────────────────────┤
│ Services │ SchedulingEngine │ Helpers   │
├─────────────────────────────────────────┤
│              数据访问层                  │
├─────────────────────────────────────────┤
│ Repositories │ DTOs │ Models           │
├─────────────────────────────────────────┤
│              SQLite 数据库               │
└─────────────────────────────────────────┘
```

### 技术栈

- **前端框架**: WinUI 3 (Windows App SDK)
- **UI架构**: MVVM (CommunityToolkit.Mvvm)
- **导航系统**: NavigationView + Frame
- **业务逻辑**: C# Services
- **数据访问**: Repository模式 + ADO.NET
- **数据库**: SQLite
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **数值计算**: MathNet.Numerics (对应需求7.3)

## 组件和接口设计

### 数据模型层

#### 核心实体模型

基于需求文档中定义的数据结构，系统核心实体模型设计如下：

```csharp
// 哨位模型 - 对应需求1.1-1.4
public class PositionLocation
{
    public int Id { get; set; }                    // 数据库ID
    public string Name { get; set; }               // 哨位名称
    public string Location { get; set; }           // 哨位地点
    public string Description { get; set; }        // 哨位介绍
    public string Requirements { get; set; }       // 哨位要求
    public List<int> RequiredSkillIds { get; set; } // 技能要求
}

// 人员模型 - 对应需求2.1-2.2
public class Personal
{
    public int Id { get; set; }                    // 数据库ID
    public string Name { get; set; }               // 姓名
    public string Position { get; set; }           // 职位
    public List<int> SkillIds { get; set; }        // 技能集合
    public bool IsAvailable { get; set; }          // 可用性状态
    public bool IsRetired { get; set; }            // 退役状态
    public int RecentShiftInterval { get; set; }   // 最近班次间隔数
    public int RecentHolidayShiftInterval { get; set; } // 最近节假日班次间隔数
    public int[] RecentTimeSlotIntervals { get; set; } // 12个时段的班次间隔数
}

// 技能模型 - 对应需求2.3
public class Skill
{
    public int Id { get; set; }                    // 技能ID
    public string Name { get; set; }               // 技能名称
    public string Description { get; set; }        // 技能描述
}

// 排班表模型 - 对应需求3.1-3.3
public class Schedule
{
    public int Id { get; set; }                    // 数据库ID
    public List<int> PersonnelIds { get; set; }    // 人员组
    public int PositionId { get; set; }            // 哨位ID
    public string Header { get; set; }             // 表头
    public List<SingleShift> Results { get; set; } // 排班结果（单次排班集合）
    public DateTime CreatedAt { get; set; }        // 创建时间
    public bool IsConfirmed { get; set; }          // 是否已确认
}

// 单次排班模型 - 对应需求3.2
public class SingleShift
{
    public int Id { get; set; }                    // 数据库ID
    public int PositionId { get; set; }            // 哨位ID
    public DateTime StartTime { get; set; }        // 班次开始时间
    public DateTime EndTime { get; set; }          // 班次结束时间
    public int PersonnelId { get; set; }           // 人员ID
    public bool IsConfirmed { get; set; }          // 是否已确认（区分缓冲区和历史区）
}
```

**设计决策说明：**
- **技能关联设计**：采用ID列表而非对象引用，便于序列化和数据库存储
- **时段间隔数组**：使用固定长度数组存储12个时段的历史间隔，支持软约束计算
- **确认状态字段**：在Schedule和SingleShift中都添加IsConfirmed字段，支持草稿和历史的分离管理

### 数据访问层

#### Repository接口设计

基于需求文档中的数据管理要求，Repository层接口设计如下：

```csharp
// 人员数据访问接口 - 对应需求2.1-2.2
public interface IPersonalRepository
{
    Task<IEnumerable<Personal>> GetAllAsync();
    Task<Personal> GetByIdAsync(int id);
    Task<int> CreateAsync(Personal personal);
    Task UpdateAsync(Personal personal);
    Task DeleteAsync(int id);
    Task<IEnumerable<Personal>> GetAvailablePersonnelAsync(); // 仅获取可用人员
    Task UpdateIntervalDataAsync(int personnelId, int[] timeSlotIntervals, int shiftInterval, int holidayInterval);
}

// 哨位数据访问接口 - 对应需求1.1-1.4
public interface IPositionLocationRepository
{
    Task<IEnumerable<PositionLocation>> GetAllAsync();
    Task<PositionLocation> GetByIdAsync(int id);
    Task<int> CreateAsync(PositionLocation position);
    Task UpdateAsync(PositionLocation position);
    Task DeleteAsync(int id);
    Task<bool> ValidateDataIntegrityAsync(PositionLocation position); // 数据完整性验证
}

// 技能数据访问接口 - 对应需求2.3
public interface ISkillRepository
{
    Task<IEnumerable<Skill>> GetAllAsync();
    Task<Skill> GetByIdAsync(int id);
    Task<int> CreateAsync(Skill skill);
    Task UpdateAsync(Skill skill);
    Task DeleteAsync(int id);
    Task<IEnumerable<Skill>> GetPersonnelSkillsAsync(int personnelId);
    Task UpdatePersonnelSkillsAsync(int personnelId, List<int> skillIds);
}

// 排班数据访问接口 - 对应需求3.1-4.4
public interface ISchedulingRepository
{
    Task<IEnumerable<Schedule>> GetAllAsync();
    Task<Schedule> GetByIdAsync(int id);
    Task<int> CreateAsync(Schedule schedule);
    Task UpdateAsync(Schedule schedule);
    Task DeleteAsync(int id);
    
    // 历史管理 - 对应需求4.1-4.4
    Task<IEnumerable<SingleShift>> GetHistoryShiftsAsync();        // 获取确定历史
    Task<IEnumerable<SingleShift>> GetBufferShiftsAsync();         // 获取历史缓冲区
    Task SaveToBufferAsync(Schedule schedule);                     // 保存到缓冲区
    Task MoveBufferToHistoryAsync();                              // 缓冲区转移到历史
    Task ClearBufferAsync();                                      // 清空缓冲区
    Task<IEnumerable<SingleShift>> GetRecentShiftsAsync(int personnelId, int days); // 获取人员近期排班
}
```

**设计决策说明：**
- **历史分离管理**：通过IsConfirmed字段和专门的方法实现缓冲区和历史区的分离，满足需求4.2-4.4
- **数据完整性验证**：在Repository层添加验证方法，确保数据质量
- **技能关联管理**：提供专门的方法管理人员与技能的多对多关系
- **性能优化接口**：提供GetRecentShiftsAsync等方法支持软约束计算的历史数据查询

### 业务逻辑层

#### 服务接口设计

基于需求文档中的业务逻辑要求，服务层接口设计如下：

```csharp
// 排班服务接口 - 对应需求3.1-3.4, 4.1-4.4
public interface ISchedulingService
{
    Task<Schedule> GenerateScheduleAsync(SchedulingRequest request);  // 生成排班方案
    Task<bool> ValidateScheduleAsync(Schedule schedule);              // 验证排班方案
    Task SaveToBufferAsync(Schedule schedule);                        // 保存到缓冲区
    Task ConfirmScheduleAsync(int scheduleId);                       // 确认排班实施
    Task<IEnumerable<Schedule>> GetDraftSchedulesAsync();            // 获取草稿排班
}

// 人员管理服务接口 - 对应需求2.1-2.2
public interface IPersonnelService
{
    Task<IEnumerable<PersonnelDto>> GetAllPersonnelAsync();
    Task<PersonnelDto> GetPersonnelByIdAsync(int id);
    Task<int> CreatePersonnelAsync(PersonnelDto personnel);
    Task UpdatePersonnelAsync(PersonnelDto personnel);
    Task DeletePersonnelAsync(int id);
    Task<IEnumerable<PersonnelDto>> GetAvailablePersonnelAsync();    // 获取可用人员
    Task<bool> ValidatePersonnelSkillsAsync(int personnelId, int positionId); // 技能匹配验证
}

// 哨位管理服务接口 - 对应需求1.1-1.4
public interface IPositionService
{
    Task<IEnumerable<PositionDto>> GetAllPositionsAsync();
    Task<PositionDto> GetPositionByIdAsync(int id);
    Task<int> CreatePositionAsync(PositionDto position);
    Task UpdatePositionAsync(PositionDto position);
    Task DeletePositionAsync(int id);
    Task<bool> ValidatePositionDataAsync(PositionDto position);      // 数据完整性验证
}

// 技能管理服务接口 - 对应需求2.3
public interface ISkillService
{
    Task<IEnumerable<SkillDto>> GetAllSkillsAsync();
    Task<SkillDto> GetSkillByIdAsync(int id);
    Task<int> CreateSkillAsync(SkillDto skill);
    Task UpdateSkillAsync(SkillDto skill);
    Task DeleteSkillAsync(int id);
    Task<IEnumerable<SkillDto>> GetPersonnelSkillsAsync(int personnelId);
    Task UpdatePersonnelSkillsAsync(int personnelId, List<int> skillIds);
}

// 历史管理服务接口 - 对应需求4.1-4.4
public interface IHistoryService
{
    Task<IEnumerable<HistoryScheduleDto>> GetHistorySchedulesAsync();
    Task<IEnumerable<SingleShift>> GetRecentShiftsAsync(int personnelId, int days);
    Task<ScheduleStatisticsDto> GetScheduleStatisticsAsync();
    Task MoveBufferToHistoryAsync();                                 // 确认排班转移
    Task ClearBufferAsync();                                        // 清空缓冲区
}
```

**设计决策说明：**
- **业务规则验证**：在服务层实现业务逻辑验证，如技能匹配、数据完整性等
- **历史管理分离**：通过专门的HistoryService管理缓冲区和历史区的数据流转
- **可用性检查**：提供专门的方法获取可用人员，支持排班算法的人员筛选
- **统计功能**：提供排班统计服务，支持软约束评分计算

### 排班引擎设计

#### 核心算法组件

基于需求文档中的算法要求，排班引擎设计如下：

```csharp
// 排班引擎接口 - 对应需求3.4, 7.1-7.5
public interface ISchedulingEngine
{
    Task<Schedule> GenerateScheduleAsync(SchedulingRequest request);
}

// 贪心排班算法实现 - 对应需求3.4, 7.5
public class GreedyScheduler : ISchedulingEngine
{
    private readonly IConstraintValidator _constraintValidator;
    private readonly ISoftConstraintCalculator _softConstraintCalculator;
    
    public async Task<Schedule> GenerateScheduleAsync(SchedulingRequest request)
    {
        // 1. 初始化三维可行张量 - 对应需求7.1, 7.2
        var feasibilityTensor = InitializeFeasibilityTensor(request);
        
        // 2. 应用硬约束 - 对应需求5.1-5.8
        ApplyHardConstraints(feasibilityTensor, request);
        
        // 3. 按照先哨位再时段的顺序分配人员 - 对应需求7.5
        var assignments = new List<Assignment>();
        for (int position = 0; position < request.Positions.Count; position++)
        {
            for (int timeSlot = 0; timeSlot < 12; timeSlot++)
            {
                var bestPersonnel = SelectBestPersonnel(
                    feasibilityTensor, position, timeSlot, request);
                if (bestPersonnel != -1)
                {
                    assignments.Add(new Assignment(position, timeSlot, bestPersonnel));
                    UpdateFeasibilityTensor(feasibilityTensor, assignments.Last());
                }
            }
        }
        
        return ConvertToSchedule(assignments, request);
    }
    
    // 三维可行张量初始化 - 对应需求7.1, 7.2
    private bool[,,] InitializeFeasibilityTensor(SchedulingRequest request)
    {
        // 维度：[哨位数量, 12个时段, 人员数量]
        var tensor = new bool[request.Positions.Count, 12, request.Personnel.Count];
        
        // 初始化为全部可行，后续通过约束筛选
        for (int p = 0; p < request.Positions.Count; p++)
        {
            for (int t = 0; t < 12; t++)
            {
                for (int person = 0; person < request.Personnel.Count; person++)
                {
                    tensor[p, t, person] = true;
                }
            }
        }
        
        return tensor;
    }
    
    // 使用逐位与运算应用硬约束 - 对应需求7.4
    private void ApplyHardConstraints(bool[,,] tensor, SchedulingRequest request)
    {
        // 应用各种硬约束，使用逐位与运算优化性能
        ApplyPersonnelAvailabilityConstraint(tensor, request);
        ApplySkillMatchConstraint(tensor, request);
        // 其他约束...
    }
}
```

**设计决策说明：**
- **三维张量设计**：采用bool[,,]数组表示哨位-时段-人员的可行性，满足需求7.1
- **二进制存储优化**：使用bool类型实现二进制存储，优化内存使用，满足需求7.2
- **逐位与运算**：通过逐位与运算处理约束条件，提升计算效率，满足需求7.4
- **分配顺序**：严格按照先哨位再时段的顺序进行分配，满足需求7.5

#### 约束处理组件

基于需求文档中的约束要求，约束处理组件设计如下：

```csharp
// 硬约束验证器接口 - 对应需求5.1-5.8
public interface IConstraintValidator
{
    bool ValidateNightShiftUniqueness(Assignment assignment, List<Assignment> existing);     // 需求5.1：夜哨唯一性
    bool ValidateNonConsecutiveShifts(Assignment assignment, List<Assignment> existing);     // 需求5.2：时段不连续
    bool ValidatePersonnelAvailability(int personnelId);                                    // 需求5.3：人员可用性
    bool ValidateSkillMatch(int personnelId, int positionId);                              // 需求5.5：技能匹配
    bool ValidateSinglePersonPerShift(Assignment assignment, List<Assignment> existing);    // 需求5.6：每哨位每时段一人
    bool ValidatePersonTimeSlotUniqueness(Assignment assignment, List<Assignment> existing); // 需求5.7：人员时段唯一性
    bool ValidateFixedAssignment(Assignment assignment, SchedulingRequest request);          // 需求5.4：定岗要求
    bool ValidateManualAssignment(Assignment assignment, SchedulingRequest request);         // 需求5.8：手动指定
}

// 软约束评分计算器接口 - 对应需求6.1-6.4
public interface ISoftConstraintCalculator
{
    double CalculateRestScore(int personnelId, int timeSlot);                               // 需求6.1：充分休息得分
    double CalculateHolidayBalanceScore(int personnelId, DateTime date);                   // 需求6.2：休息日平衡得分
    double CalculateTimeSlotBalanceScore(int personnelId, int timeSlot);                   // 需求6.3：时段平衡得分
    double CalculateTotalScore(int personnelId, int timeSlot, DateTime date);              // 需求6.4：综合评分
}

// 硬约束验证器实现
public class ConstraintValidator : IConstraintValidator
{
    private readonly IPersonnelService _personnelService;
    private readonly IPositionService _positionService;
    
    // 需求5.1：确保一个人同一个晚上只能上一个夜哨
    public bool ValidateNightShiftUniqueness(Assignment assignment, List<Assignment> existing)
    {
        if (!IsNightShift(assignment.TimeSlot)) return true;
        
        return !existing.Any(a => a.PersonnelId == assignment.PersonnelId && 
                                 IsNightShift(a.TimeSlot) && 
                                 IsSameNight(assignment, a));
    }
    
    // 需求5.2：防止一个人在相邻时段连续上哨
    public bool ValidateNonConsecutiveShifts(Assignment assignment, List<Assignment> existing)
    {
        var consecutiveSlots = GetConsecutiveTimeSlots(assignment.TimeSlot);
        return !existing.Any(a => a.PersonnelId == assignment.PersonnelId && 
                                 consecutiveSlots.Contains(a.TimeSlot));
    }
    
    // 需求5.3：仅为可用状态的人员分配哨位
    public bool ValidatePersonnelAvailability(int personnelId)
    {
        var personnel = _personnelService.GetPersonnelByIdAsync(personnelId).Result;
        return personnel != null && personnel.IsAvailable && !personnel.IsRetired;
    }
    
    // 需求5.5：验证人员技能与哨位要求的匹配性
    public bool ValidateSkillMatch(int personnelId, int positionId)
    {
        return _personnelService.ValidatePersonnelSkillsAsync(personnelId, positionId).Result;
    }
}

// 软约束评分计算器实现
public class SoftConstraintCalculator : ISoftConstraintCalculator
{
    private readonly IHistoryService _historyService;
    
    // 需求6.1：计算充分休息得分，基于人员距离上次分配的时段间隔
    public double CalculateRestScore(int personnelId, int timeSlot)
    {
        var personnel = GetPersonnelById(personnelId);
        var interval = personnel.RecentTimeSlotIntervals[timeSlot];
        
        // 间隔越长，得分越高
        return Math.Min(interval / 7.0, 1.0); // 7天为满分
    }
    
    // 需求6.2：计算休息日平衡得分
    public double CalculateHolidayBalanceScore(int personnelId, DateTime date)
    {
        if (!IsHoliday(date)) return 0.5; // 非休息日给中等分
        
        var personnel = GetPersonnelById(personnelId);
        var holidayInterval = personnel.RecentHolidayShiftInterval;
        
        // 休息日班次间隔越长，得分越高
        return Math.Min(holidayInterval / 30.0, 1.0); // 30天为满分
    }
    
    // 需求6.3：计算各时段平衡得分
    public double CalculateTimeSlotBalanceScore(int personnelId, int timeSlot)
    {
        var personnel = GetPersonnelById(personnelId);
        var timeSlotInterval = personnel.RecentTimeSlotIntervals[timeSlot];
        
        // 该时段间隔越长，得分越高
        return Math.Min(timeSlotInterval / 14.0, 1.0); // 14天为满分
    }
}
```

**设计决策说明：**
- **完整约束覆盖**：实现了需求文档中定义的所有8个硬约束和4个软约束
- **性能优化设计**：约束验证使用高效的查找算法，支持大规模数据处理
- **评分算法设计**：软约束评分采用归一化设计，便于权重调整和综合评分
- **依赖注入支持**：通过接口依赖其他服务，保持组件间的松耦合

### 用户界面层

#### ViewModel设计

```csharp
public partial class SchedulingViewModel : ObservableObject
{
    private readonly ISchedulingService _schedulingService;
    private readonly IPersonnelService _personnelService;
    
    [ObservableProperty]
    private ObservableCollection<PersonnelDto> availablePersonnel;
    
    [ObservableProperty]
    private ObservableCollection<PositionDto> positions;
    
    [ObservableProperty]
    private ScheduleDto currentSchedule;
    
    [RelayCommand]
    private async Task GenerateScheduleAsync()
    {
        // 生成排班逻辑
    }
    
    [RelayCommand]
    private async Task ConfirmScheduleAsync()
    {
        // 确认排班逻辑
    }
}
```

## 数据模型设计

### 数据库架构

基于需求文档中的数据存储要求，数据库架构设计如下：

```sql
-- 哨位表 - 对应需求1.1-1.4
CREATE TABLE PositionLocations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,                    -- 哨位名称
    Location TEXT,                         -- 哨位地点
    Description TEXT,                      -- 哨位介绍
    Requirements TEXT,                     -- 哨位要求
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 人员表 - 对应需求2.1-2.2
CREATE TABLE Personnel (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,                    -- 姓名
    Position TEXT,                         -- 职位
    IsAvailable BOOLEAN DEFAULT 1,         -- 可用性状态
    IsRetired BOOLEAN DEFAULT 0,           -- 退役状态
    RecentShiftInterval INTEGER DEFAULT 0, -- 最近班次间隔数
    RecentHolidayShiftInterval INTEGER DEFAULT 0, -- 最近节假日班次间隔数
    TimeSlotIntervals TEXT,                -- JSON数组存储12个时段间隔
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 技能表 - 对应需求2.3
CREATE TABLE Skills (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,                    -- 技能名称
    Description TEXT,                      -- 技能描述
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 人员技能关联表 - 对应需求2.4
CREATE TABLE PersonnelSkills (
    PersonnelId INTEGER,
    SkillId INTEGER,
    AssignedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (PersonnelId, SkillId),
    FOREIGN KEY (PersonnelId) REFERENCES Personnel(Id) ON DELETE CASCADE,
    FOREIGN KEY (SkillId) REFERENCES Skills(Id) ON DELETE CASCADE
);

-- 排班历史表 - 对应需求4.1-4.4
CREATE TABLE ShiftHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PositionId INTEGER NOT NULL,
    PersonnelId INTEGER NOT NULL,
    StartTime DATETIME NOT NULL,           -- 班次开始时间
    EndTime DATETIME NOT NULL,             -- 班次结束时间
    IsConfirmed BOOLEAN DEFAULT 0,         -- 0为缓冲区，1为确定历史
    ScheduleId INTEGER,                    -- 关联的排班表ID
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    ConfirmedAt DATETIME,                  -- 确认时间
    FOREIGN KEY (PositionId) REFERENCES PositionLocations(Id),
    FOREIGN KEY (PersonnelId) REFERENCES Personnel(Id)
);

-- 排班表主表 - 对应需求3.1
CREATE TABLE Schedules (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Header TEXT NOT NULL,                  -- 表头
    PersonnelIds TEXT,                     -- JSON数组存储人员组
    PositionId INTEGER,                    -- 主要哨位ID（可选）
    IsConfirmed BOOLEAN DEFAULT 0,         -- 是否已确认
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    ConfirmedAt DATETIME,
    FOREIGN KEY (PositionId) REFERENCES PositionLocations(Id)
);

-- 索引优化
CREATE INDEX idx_personnel_available ON Personnel(IsAvailable, IsRetired);
CREATE INDEX idx_shift_history_confirmed ON ShiftHistory(IsConfirmed);
CREATE INDEX idx_shift_history_personnel_time ON ShiftHistory(PersonnelId, StartTime);
CREATE INDEX idx_shift_history_position_time ON ShiftHistory(PositionId, StartTime);
CREATE INDEX idx_personnel_skills_personnel ON PersonnelSkills(PersonnelId);
CREATE INDEX idx_personnel_skills_skill ON PersonnelSkills(SkillId);
```

**设计决策说明：**
- **历史分离存储**：通过IsConfirmed字段实现缓冲区和确定历史的分离，满足需求4.2
- **时间戳管理**：添加CreatedAt、UpdatedAt、ConfirmedAt等时间戳字段，支持历史追踪
- **外键约束**：使用CASCADE删除确保数据一致性
- **索引优化**：针对查询热点添加复合索引，提升查询性能
- **JSON存储**：使用TEXT字段存储JSON格式的数组数据，平衡存储效率和查询便利性

### 数据传输对象(DTOs)

基于需求文档中的数据流转要求，DTO设计如下：

```csharp
// 排班请求DTO - 对应需求3.1-3.4
public class SchedulingRequest
{
    public List<PositionDto> Positions { get; set; }
    public List<PersonnelDto> Personnel { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SchedulingStrategy Strategy { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public List<ManualAssignmentDto> ManualAssignments { get; set; }  // 手动指定
    public List<FixedAssignmentDto> FixedAssignments { get; set; }    // 定岗要求
}

// 排班表DTO - 对应需求3.1, 4.1-4.4
public class ScheduleDto
{
    public int Id { get; set; }
    public string Header { get; set; }
    public List<int> PersonnelIds { get; set; }
    public int? PositionId { get; set; }
    public List<SingleShiftDto> Results { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public bool IsConfirmed { get; set; }
}

// 单次排班DTO - 对应需求3.2
public class SingleShiftDto
{
    public int Id { get; set; }
    public int PositionId { get; set; }
    public string PositionName { get; set; }
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsConfirmed { get; set; }
    public int TimeSlot { get; set; }  // 0-11表示12个时段
}

// 人员DTO - 对应需求2.1-2.2
public class PersonnelDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public List<SkillDto> Skills { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsRetired { get; set; }
    public int RecentShiftInterval { get; set; }
    public int RecentHolidayShiftInterval { get; set; }
    public int[] RecentTimeSlotIntervals { get; set; }
}

// 哨位DTO - 对应需求1.1-1.4
public class PositionDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
    public string Description { get; set; }
    public string Requirements { get; set; }
    public List<SkillDto> RequiredSkills { get; set; }
}

// 技能DTO - 对应需求2.3
public class SkillDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

// 历史排班DTO - 对应需求4.1-4.4
public class HistoryScheduleDto
{
    public int Id { get; set; }
    public string Header { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public int TotalShifts { get; set; }
    public int PersonnelCount { get; set; }
    public List<SingleShiftDto> Shifts { get; set; }
}

// 排班统计DTO - 对应需求6.1-6.4
public class ScheduleStatisticsDto
{
    public int TotalSchedules { get; set; }
    public int ConfirmedSchedules { get; set; }
    public int DraftSchedules { get; set; }
    public Dictionary<int, int> PersonnelShiftCounts { get; set; }
    public Dictionary<int, double> TimeSlotDistribution { get; set; }
    public DateTime LastScheduleDate { get; set; }
}

// 手动指定DTO - 对应需求5.8
public class ManualAssignmentDto
{
    public int PositionId { get; set; }
    public int TimeSlot { get; set; }
    public int PersonnelId { get; set; }
    public DateTime Date { get; set; }
}

// 定岗要求DTO - 对应需求5.4
public class FixedAssignmentDto
{
    public int PersonnelId { get; set; }
    public List<int> AllowedPositionIds { get; set; }
    public List<int> AllowedTimeSlots { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
```

**设计决策说明：**
- **完整性保证**：DTO包含了需求文档中定义的所有必要字段
- **关联数据展示**：在DTO中包含关联对象的名称等展示信息，减少前端查询
- **约束支持**：添加ManualAssignmentDto和FixedAssignmentDto支持硬约束需求
- **统计信息**：提供ScheduleStatisticsDto支持软约束评分和系统监控

## 错误处理

### 异常处理策略

1. **数据验证异常**: 在Service层进行业务规则验证
2. **数据库异常**: Repository层捕获并转换为业务异常
3. **算法异常**: 排班引擎处理约束冲突和无解情况
4. **UI异常**: ViewModel层处理用户操作异常

### 错误类型定义

```csharp
public class SchedulingException : Exception
{
    public SchedulingErrorType ErrorType { get; }
    public string Details { get; }
}

public enum SchedulingErrorType
{
    ConstraintViolation,
    InsufficientPersonnel,
    SkillMismatch,
    DataValidationError,
    DatabaseError
}
```

## 测试策略

### 单元测试

- **Repository层**: 数据访问逻辑测试
- **Service层**: 业务逻辑测试
- **排班引擎**: 算法正确性测试
- **约束验证**: 硬约束和软约束测试

### 集成测试

- **数据库集成**: SQLite数据操作测试
- **服务集成**: 跨层调用测试
- **UI集成**: ViewModel与View交互测试

### 性能测试

- **大规模数据**: 测试1000+人员、100+哨位的排班性能
- **算法效率**: 可行张量计算性能测试
- **内存使用**: 长时间运行的内存泄漏测试

### 测试数据

基于需求文档中的测试要求，测试数据构建器设计如下：

```csharp
public class TestDataBuilder
{
    // 生成测试人员数据 - 覆盖需求2.1-2.2的各种场景
    public static List<Personal> CreateTestPersonnel(int count)
    {
        var personnel = new List<Personal>();
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            personnel.Add(new Personal
            {
                Id = i + 1,
                Name = $"人员{i + 1:D3}",
                Position = GetRandomPosition(),
                SkillIds = GetRandomSkills(random),
                IsAvailable = random.NextDouble() > 0.1, // 90%可用
                IsRetired = random.NextDouble() < 0.05,  // 5%退役
                RecentShiftInterval = random.Next(0, 30),
                RecentHolidayShiftInterval = random.Next(0, 60),
                RecentTimeSlotIntervals = GenerateTimeSlotIntervals(random)
            });
        }
        
        return personnel;
    }
    
    // 生成测试哨位数据 - 覆盖需求1.1-1.4的各种场景
    public static List<PositionLocation> CreateTestPositions(int count)
    {
        var positions = new List<PositionLocation>();
        var locations = new[] { "东门", "西门", "南门", "北门", "中央", "后勤" };
        
        for (int i = 0; i < count; i++)
        {
            positions.Add(new PositionLocation
            {
                Id = i + 1,
                Name = $"{locations[i % locations.Length]}哨位{i + 1}",
                Location = locations[i % locations.Length],
                Description = $"负责{locations[i % locations.Length]}区域的安全警戒",
                Requirements = GetRandomRequirements(),
                RequiredSkillIds = GetRandomRequiredSkills()
            });
        }
        
        return positions;
    }
    
    // 生成测试排班请求 - 覆盖需求3.1-3.4的各种场景
    public static SchedulingRequest CreateTestRequest()
    {
        return new SchedulingRequest
        {
            Positions = CreateTestPositions(10).Select(p => MapToDto(p)).ToList(),
            Personnel = CreateTestPersonnel(50).Select(p => MapToDto(p)).ToList(),
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            Strategy = SchedulingStrategy.Greedy,
            Parameters = new Dictionary<string, object>
            {
                ["RestWeight"] = 0.4,
                ["HolidayWeight"] = 0.3,
                ["TimeSlotWeight"] = 0.3
            },
            ManualAssignments = CreateTestManualAssignments(),
            FixedAssignments = CreateTestFixedAssignments()
        };
    }
    
    // 生成约束测试数据 - 覆盖需求5.1-5.8的各种约束场景
    public static List<TestConstraintScenario> CreateConstraintTestScenarios()
    {
        return new List<TestConstraintScenario>
        {
            // 夜哨唯一性测试场景
            new TestConstraintScenario
            {
                Name = "夜哨唯一性约束",
                Description = "测试一个人同一晚上只能上一个夜哨",
                TestData = CreateNightShiftConflictData()
            },
            // 时段连续性测试场景
            new TestConstraintScenario
            {
                Name = "时段连续性约束",
                Description = "测试防止相邻时段连续排班",
                TestData = CreateConsecutiveShiftData()
            },
            // 技能匹配测试场景
            new TestConstraintScenario
            {
                Name = "技能匹配约束",
                Description = "测试人员技能与哨位要求匹配",
                TestData = CreateSkillMismatchData()
            }
        };
    }
    
    // 生成性能测试数据 - 覆盖需求7.1-7.5的性能要求
    public static PerformanceTestData CreatePerformanceTestData()
    {
        return new PerformanceTestData
        {
            LargeScalePersonnel = CreateTestPersonnel(1000),  // 1000人员
            LargeScalePositions = CreateTestPositions(100),   // 100哨位
            ComplexConstraints = CreateComplexConstraints(),
            ExpectedMaxExecutionTime = TimeSpan.FromMinutes(5), // 5分钟内完成
            ExpectedMaxMemoryUsage = 500 * 1024 * 1024 // 500MB内存限制
        };
    }
    
    // 辅助方法
    private static int[] GenerateTimeSlotIntervals(Random random)
    {
        var intervals = new int[12];
        for (int i = 0; i < 12; i++)
        {
            intervals[i] = random.Next(0, 30); // 0-30天间隔
        }
        return intervals;
    }
    
    private static List<int> GetRandomSkills(Random random)
    {
        var allSkills = Enumerable.Range(1, 10).ToList(); // 假设有10种技能
        var skillCount = random.Next(1, 4); // 每人1-3个技能
        return allSkills.OrderBy(x => random.Next()).Take(skillCount).ToList();
    }
}

// 测试场景数据结构
public class TestConstraintScenario
{
    public string Name { get; set; }
    public string Description { get; set; }
    public object TestData { get; set; }
}

public class PerformanceTestData
{
    public List<Personal> LargeScalePersonnel { get; set; }
    public List<PositionLocation> LargeScalePositions { get; set; }
    public object ComplexConstraints { get; set; }
    public TimeSpan ExpectedMaxExecutionTime { get; set; }
    public long ExpectedMaxMemoryUsage { get; set; }
}
```

**设计决策说明：**
- **全面场景覆盖**：测试数据覆盖了需求文档中的所有功能场景
- **约束测试支持**：专门的约束测试场景生成，确保硬约束和软约束的正确性
- **性能测试数据**：提供大规模数据生成，验证需求7.1-7.5的性能要求
- **随机性与确定性平衡**：使用随机数生成多样化数据，同时保证测试的可重现性