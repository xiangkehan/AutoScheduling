# FileLocationManager - 文件位置管理器

## 概述

`FileLocationManager` 是一个用于管理测试数据文件存储位置和历史记录的类，完全符合 WinUI3 规范。它提供了文件创建、最近文件管理和旧文件清理等功能。

## 主要功能

### 1. 文件位置管理

- **默认存储位置**: 使用 `ApplicationData.Current.LocalFolder` + `TestData` 子文件夹
- **自动创建文件夹**: 如果 TestData 文件夹不存在，会自动创建
- **文件名生成**: 自动生成带时间戳的文件名（格式：`test-data-yyyyMMdd-HHmmss.json`）

### 2. 最近文件管理

- **FutureAccessList**: 使用 Windows Storage API 的 `FutureAccessList` 保存文件访问令牌
- **LocalSettings 持久化**: 使用 `ApplicationData.Current.LocalSettings` 持久化最近文件列表
- **自动清理**: 限制最多保存 20 个最近文件，超出部分自动清理
- **失效检测**: 自动检测并移除已删除或不可访问的文件

### 3. 文件清理

- **按天数清理**: 支持清理指定天数之前的旧文件
- **安全删除**: 删除失败不会影响其他文件的清理
- **日志记录**: 记录清理操作的详细信息

## 使用示例

### 基本使用

```csharp
using AutoScheduling3.Data.Logging;
using AutoScheduling3.TestData;

// 创建日志记录器
ILogger logger = new YourLogger();

// 创建文件位置管理器
var fileManager = new FileLocationManager(logger);

// 创建新的测试数据文件
var file = await fileManager.CreateNewTestDataFileAsync();
Console.WriteLine($"创建文件: {file.Name}");

// 写入数据（使用 TestDataGenerator）
var generator = new TestDataGenerator();
await generator.ExportToStorageFileAsync(file);

// 添加到最近文件列表
await fileManager.AddToRecentFilesAsync(file);
```

### 获取最近文件

```csharp
// 获取最近生成的文件列表
var recentFiles = await fileManager.GetRecentTestDataFilesAsync();

foreach (var fileInfo in recentFiles)
{
    Console.WriteLine($"文件: {fileInfo.FileName}");
    Console.WriteLine($"  大小: {fileInfo.FormattedSize}");
    Console.WriteLine($"  时间: {fileInfo.FormattedDate}");
    Console.WriteLine($"  路径: {fileInfo.FilePath}");
}
```

### 清理旧文件

```csharp
// 清理 30 天前的文件
int deletedCount = await fileManager.CleanOldFilesAsync(daysToKeep: 30);
Console.WriteLine($"已清理 {deletedCount} 个旧文件");

// 清理 7 天前的文件（更激进的清理）
deletedCount = await fileManager.CleanOldFilesAsync(daysToKeep: 7);
Console.WriteLine($"已清理 {deletedCount} 个旧文件");
```

### 完整工作流程

```csharp
// 1. 创建管理器
var fileManager = new FileLocationManager(logger);

// 2. 创建并导出测试数据
var generator = new TestDataGenerator();
var file = await fileManager.CreateNewTestDataFileAsync();
await generator.ExportToStorageFileAsync(file);

// 3. 添加到最近文件
await fileManager.AddToRecentFilesAsync(file);

// 4. 查看最近文件
var recentFiles = await fileManager.GetRecentTestDataFilesAsync();
Console.WriteLine($"最近文件数量: {recentFiles.Count}");

// 5. 定期清理旧文件
await fileManager.CleanOldFilesAsync(daysToKeep: 30);
```

## API 参考

### 构造函数

```csharp
public FileLocationManager(ILogger logger)
```

创建 FileLocationManager 实例。

**参数:**
- `logger`: 日志记录器接口

### 方法

#### GetTestDataFolderAsync

```csharp
public async Task<StorageFolder> GetTestDataFolderAsync()
```

获取或创建默认测试数据文件夹。

**返回值:** 测试数据文件夹的 `StorageFolder` 对象

#### GenerateNewFileName

```csharp
public string GenerateNewFileName()
```

生成新的文件名（带时间戳）。

**返回值:** 格式为 `test-data-yyyyMMdd-HHmmss.json` 的文件名

#### CreateNewTestDataFileAsync

```csharp
public async Task<StorageFile> CreateNewTestDataFileAsync()
```

在默认测试数据文件夹中创建新文件。

**返回值:** 新创建的 `StorageFile` 对象

#### AddToRecentFilesAsync

```csharp
public async Task AddToRecentFilesAsync(StorageFile file)
```

将文件添加到最近文件列表。

**参数:**
- `file`: 要添加的文件

**说明:**
- 自动添加到 FutureAccessList 以保持访问权限
- 移除重复项
- 限制最多 20 个文件

#### GetRecentTestDataFilesAsync

```csharp
public async Task<List<GeneratedFileInfo>> GetRecentTestDataFilesAsync()
```

获取最近生成的文件列表。

**返回值:** `GeneratedFileInfo` 对象列表

**说明:**
- 自动检测并移除不可访问的文件
- 返回的列表按添加时间倒序排列

#### CleanOldFilesAsync

```csharp
public async Task<int> CleanOldFilesAsync(int daysToKeep = 30)
```

清理旧文件。

**参数:**
- `daysToKeep`: 保留的天数，默认 30 天

**返回值:** 删除的文件数量

**异常:**
- `ArgumentException`: 当 `daysToKeep` 为负数时抛出

## 数据模型

### RecentFileInfo

用于 LocalSettings 存储的最近文件信息。

```csharp
public class RecentFileInfo
{
    public string Token { get; set; }           // FutureAccessList 令牌
    public string FileName { get; set; }        // 文件名
    public string FilePath { get; set; }        // 文件路径
    public DateTime GeneratedAt { get; set; }   // 生成时间
}
```

### GeneratedFileInfo

用于 UI 显示的文件信息。

```csharp
public class GeneratedFileInfo
{
    public string FileName { get; set; }        // 文件名
    public string FilePath { get; set; }        // 文件路径
    public DateTime GeneratedAt { get; set; }   // 生成时间
    public long FileSize { get; set; }          // 文件大小（字节）
    public StorageFile? StorageFile { get; set; } // StorageFile 引用
    
    // 格式化属性
    public string FormattedSize { get; }        // 格式化的文件大小（如 "1.5 MB"）
    public string FormattedDate { get; }        // 格式化的日期（如 "2024-11-09 14:30:45"）
}
```

## 存储位置

### 默认文件夹

```
C:\Users\{Username}\AppData\Local\Packages\{PackageFamilyName}\LocalState\TestData\
```

### LocalSettings 键

- **RecentFilesKey**: `TestDataGenerator_RecentFiles`
  - 存储最近文件列表的 JSON 字符串

### FutureAccessList

- 使用 `StorageApplicationPermissions.FutureAccessList` 保存文件访问令牌
- 每个文件都有唯一的令牌
- 最多保存 20 个文件的令牌

## 注意事项

1. **WinUI3 兼容性**: 完全使用 WinUI3 的 Storage API，不依赖传统的文件系统 API
2. **权限管理**: 使用 FutureAccessList 确保应用重启后仍能访问文件
3. **错误处理**: 所有方法都包含完善的错误处理和日志记录
4. **线程安全**: 方法都是异步的，适合在 UI 线程中调用
5. **资源清理**: 自动清理无效的文件引用和超出限制的历史记录

## 测试

运行测试以验证功能：

```csharp
using AutoScheduling3.Tests;

// 运行所有 FileLocationManager 测试
await FileLocationManagerTests.RunAllTestsAsync();
```

## 相关类

- `TestDataGenerator`: 测试数据生成器
- `TestDataConfiguration`: 测试数据配置
- `SampleDataProvider`: 示例数据提供者

## 更新日志

### v1.0.0 (2024-11-09)

- 初始版本
- 实现基本文件位置管理
- 实现最近文件管理（使用 FutureAccessList）
- 实现文件清理功能
- 添加完整的测试套件
