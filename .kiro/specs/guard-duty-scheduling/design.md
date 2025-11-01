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
- **数值计算**: MathNet.Numerics

## 组件和接口设计

### 数据模型层

#### 核心实体模型

```csharp
// 哨位模型
public class PositionLocation
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
    public string Description { get; set; }
    public string Requirements { get; set; }
}

// 人员模型
public class Personal
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public List<int> SkillIds { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsRetired { get; set; }
    public int RecentShiftInterval { get; set; }
    public int RecentHolidayShiftInterval { get; set; }
    public int[] RecentTimeSlotIntervals { get; set; } // 12个时段
}

// 技能模型
public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

// 排班表模型
public class Schedule
{
    public int Id { get; set; }
    public List<SingleShift> Shifts { get; set; }
    public List<int> PersonnelIds { get; set; }
    public int PositionId { get; set; }
    public string Header { get; set; }
    public List<SingleShift> Results { get; set; }
}

// 单次排班模型
public class SingleShift
{
    public int Id { get; set; }
    public int PositionId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int PersonnelId { get; set; }
}
```

### 数据访问层

#### Repository接口设计

```csharp
public interface IPersonalRepository
{
    Task<IEnumerable<Personal>> GetAllAsync();
    Task<Personal> GetByIdAsync(int id);
    Task<int> CreateAsync(Personal personal);
    Task UpdateAsync(Personal personal);
    Task DeleteAsync(int id);
    Task<IEnumerable<Personal>> GetAvailablePersonnelAsync();
}

public interface IPositionLocationRepository
{
    Task<IEnumerable<PositionLocation>> GetAllAsync();
    Task<PositionLocation> GetByIdAsync(int id);
    Task<int> CreateAsync(PositionLocation position);
    Task UpdateAsync(PositionLocation position);
    Task DeleteAsync(int id);
}

public interface ISchedulingRepository
{
    Task<IEnumerable<Schedule>> GetAllAsync();
    Task<Schedule> GetByIdAsync(int id);
    Task<int> CreateAsync(Schedule schedule);
    Task UpdateAsync(Schedule schedule);
    Task DeleteAsync(int id);
    Task<IEnumerable<SingleShift>> GetHistoryShiftsAsync();
    Task<IEnumerable<SingleShift>> GetBufferShiftsAsync();
    Task MoveBufferToHistoryAsync();
    Task ClearBufferAsync();
}
```

### 业务逻辑层

#### 服务接口设计

```csharp
public interface ISchedulingService
{
    Task<Schedule> GenerateScheduleAsync(SchedulingRequest request);
    Task<bool> ValidateScheduleAsync(Schedule schedule);
    Task SaveToBufferAsync(Schedule schedule);
    Task ConfirmScheduleAsync(int scheduleId);
}

public interface IPersonnelService
{
    Task<IEnumerable<PersonnelDto>> GetAllPersonnelAsync();
    Task<PersonnelDto> GetPersonnelByIdAsync(int id);
    Task<int> CreatePersonnelAsync(PersonnelDto personnel);
    Task UpdatePersonnelAsync(PersonnelDto personnel);
    Task DeletePersonnelAsync(int id);
}

public interface IHistoryService
{
    Task<IEnumerable<HistoryScheduleDto>> GetHistorySchedulesAsync();
    Task<IEnumerable<SingleShift>> GetRecentShiftsAsync(int personnelId, int days);
    Task<ScheduleStatisticsDto> GetScheduleStatisticsAsync();
}
```

### 排班引擎设计

#### 核心算法组件

```csharp
public interface ISchedulingEngine
{
    Task<Schedule> GenerateScheduleAsync(SchedulingRequest request);
}

public class GreedyScheduler : ISchedulingEngine
{
    private readonly IConstraintValidator _constraintValidator;
    private readonly ISoftConstraintCalculator _softConstraintCalculator;
    
    public async Task<Schedule> GenerateScheduleAsync(SchedulingRequest request)
    {
        // 1. 初始化可行张量
        var feasibilityTensor = InitializeFeasibilityTensor(request);
        
        // 2. 应用硬约束
        ApplyHardConstraints(feasibilityTensor, request);
        
        // 3. 按顺序分配人员
        var assignments = new List<Assignment>();
        for (int position = 0; position < request.Positions.Count; position++)
        {
            for (int timeSlot = 0; timeSlot < 12; timeSlot++)
            {
                var bestPersonnel = SelectBestPersonnel(
                    feasibilityTensor, position, timeSlot, request);
                assignments.Add(new Assignment(position, timeSlot, bestPersonnel));
                UpdateFeasibilityTensor(feasibilityTensor, assignments.Last());
            }
        }
        
        return ConvertToSchedule(assignments, request);
    }
}
```

#### 约束处理组件

```csharp
public interface IConstraintValidator
{
    bool ValidateNightShiftUniqueness(Assignment assignment, List<Assignment> existing);
    bool ValidateNonConsecutiveShifts(Assignment assignment, List<Assignment> existing);
    bool ValidatePersonnelAvailability(int personnelId);
    bool ValidateSkillMatch(int personnelId, int positionId);
    bool ValidateSinglePersonPerShift(Assignment assignment, List<Assignment> existing);
}

public interface ISoftConstraintCalculator
{
    double CalculateRestScore(int personnelId, int timeSlot);
    double CalculateHolidayBalanceScore(int personnelId, DateTime date);
    double CalculateTimeSlotBalanceScore(int personnelId, int timeSlot);
    double CalculateTotalScore(int personnelId, int timeSlot, DateTime date);
}
```

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

```sql
-- 哨位表
CREATE TABLE PositionLocations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Location TEXT,
    Description TEXT,
    Requirements TEXT
);

-- 人员表
CREATE TABLE Personnel (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Position TEXT,
    IsAvailable BOOLEAN DEFAULT 1,
    IsRetired BOOLEAN DEFAULT 0,
    RecentShiftInterval INTEGER DEFAULT 0,
    RecentHolidayShiftInterval INTEGER DEFAULT 0,
    TimeSlotIntervals TEXT -- JSON数组存储12个时段间隔
);

-- 技能表
CREATE TABLE Skills (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT
);

-- 人员技能关联表
CREATE TABLE PersonnelSkills (
    PersonnelId INTEGER,
    SkillId INTEGER,
    PRIMARY KEY (PersonnelId, SkillId),
    FOREIGN KEY (PersonnelId) REFERENCES Personnel(Id),
    FOREIGN KEY (SkillId) REFERENCES Skills(Id)
);

-- 排班历史表
CREATE TABLE ShiftHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PositionId INTEGER,
    PersonnelId INTEGER,
    StartTime DATETIME,
    EndTime DATETIME,
    IsConfirmed BOOLEAN DEFAULT 0, -- 0为缓冲区，1为确定历史
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (PositionId) REFERENCES PositionLocations(Id),
    FOREIGN KEY (PersonnelId) REFERENCES Personnel(Id)
);
```

### 数据传输对象(DTOs)

```csharp
public class SchedulingRequest
{
    public List<PositionDto> Positions { get; set; }
    public List<PersonnelDto> Personnel { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SchedulingStrategy Strategy { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}

public class ScheduleDto
{
    public int Id { get; set; }
    public string Header { get; set; }
    public List<SingleShiftDto> Shifts { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsConfirmed { get; set; }
}
```

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

```csharp
public class TestDataBuilder
{
    public static List<Personal> CreateTestPersonnel(int count)
    {
        // 生成测试人员数据
    }
    
    public static List<PositionLocation> CreateTestPositions(int count)
    {
        // 生成测试哨位数据
    }
    
    public static SchedulingRequest CreateTestRequest()
    {
        // 生成测试排班请求
    }
}
```