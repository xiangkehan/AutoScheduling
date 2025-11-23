# 设计文档

## 概述

本设计文档描述了人员选择器增强功能的技术实现方案。该功能将创建一个统一的、可复用的人员选择器控件，支持下拉选择和智能搜索（包括模糊匹配和拼音首字母匹配），并在多个场景中提供一致的用户体验。

### 使用场景分析

通过代码分析，发现以下场景需要人员选择功能：

1. **ScheduleResultPage.xaml**（排班结果页面）
   - 当前实现：AutoSuggestBox用于人员筛选
   - 状态：已实现基础功能，需要增强模糊匹配
   - 位置：筛选工具栏

2. **EditShiftAssignmentDialog.xaml**（修改班次分配对话框）
   - 当前实现：简单的AutoSuggestBox + ListView
   - 状态：需要增强搜索功能和双向同步
   - 位置：对话框主体

3. **CreateSchedulingPage.xaml**（创建排班页面）
   - 当前实现：ListView双列表选择（可用人员 ↔ 已选人员）
   - 状态：可以添加搜索功能提升体验
   - 位置：步骤3 - 选择参与人员

4. **TemplatePage.xaml**（模板页面）
   - 当前实现：ListView双列表选择（可用人员 ↔ 已选人员）
   - 状态：可以添加搜索功能提升体验
   - 位置：模板详情面板 - 参与人员部分

### 实施优先级

1. **高优先级**：EditShiftAssignmentDialog（用户痛点最明显）
2. **中优先级**：ScheduleResultPage（已有基础，增强即可）
3. **低优先级**：CreateSchedulingPage、TemplatePage（可选优化）

## 架构

### 组件层次结构

基于WinUI 3原生控件的轻量级方案：

```
AutoSuggestBox (WinUI 3原生控件)
├── PersonnelSelectorBehavior (可选：封装可复用逻辑)
├── FuzzyMatcher (模糊匹配算法)
├── PinyinHelper (拼音转换)
└── ViewModel (业务逻辑)
```

### 数据流

```
用户输入 → AutoSuggestBox.TextChanged
         → ViewModel.UpdateSuggestions()
         → FuzzyMatcher.Match()
         → 更新建议列表
         → 用户选择 → 触发 PersonnelSelected 事件
```

## 组件和接口

### 1. FuzzyMatcher 类（核心算法）

负责实现增强的模糊匹配算法。

**接口：**
```csharp
public static class FuzzyMatcher
{
    // 主匹配方法
    public static List<MatchResult> Match(
        string query, 
        IEnumerable<PersonnelDto> candidates,
        FuzzyMatchOptions? options = null);
    
    // 匹配单个候选人
    private static MatchResult MatchSingle(
        string normalizedQuery,
        PersonnelDto candidate,
        FuzzyMatchOptions options);
    
    // 标准化文本
    private static string NormalizeText(string text, bool removePunctuation = true);
    
    // 计算编辑距离
    private static int CalculateEditDistance(string source, string target);
    
    // 拼音完全匹配
    private static bool MatchFullPinyin(string query, string name);
    
    // 拼音前缀匹配
    private static bool MatchPinyinPrefix(string query, string name);
    
    // 拼音首字母完全匹配
    private static bool MatchPinyinInitialsExact(string query, string name);
    
    // 拼音首字母前缀匹配
    private static bool MatchPinyinInitialsPrefix(string query, string name);
    
    // 序列模糊匹配
    private static bool FuzzySequenceMatch(string query, string target);
    
    // 计算精细化分数
    private static int CalculateFinalScore(
        int baseScore,
        string query,
        string target,
        int matchPosition,
        FuzzyMatchOptions options);
    
    // 计算长度相似度
    private static double CalculateLengthSimilarity(int queryLength, int targetLength);
    
    // 计算匹配位置加成
    private static int CalculatePositionBonus(int position, int targetLength);
    
    // 计算连续匹配加成
    private static int CalculateContinuousMatchBonus(string query, string target);
}
```


### 2. PinyinHelper 类（拼音转换）

负责中文转拼音和拼音首字母提取，支持缓存优化。

**接口：**
```csharp
public static class PinyinHelper
{
    // 获取拼音首字母
    public static string GetPinyinInitials(string chinese);
    
    // 获取完整拼音（无空格连续）
    public static string GetFullPinyin(string chinese);
    
    // 获取完整拼音（带空格分隔）
    public static string GetFullPinyinWithSeparator(string chinese);
    
    // 判断字符是否为中文
    public static bool IsChinese(char c);
    
    // 清空缓存
    public static void ClearCache();
    
    // 获取缓存统计信息
    public static (int InitialsCount, int FullPinyinCount) GetCacheStats();
    
    // 拼音缓存
    private static Dictionary<string, string> _pinyinInitialsCache;
    private static Dictionary<string, string> _fullPinyinCache;
    private static object _cacheLock;
}
```

### 3. PersonnelSearchHelper 类（搜索辅助）

封装搜索逻辑，可在多个ViewModel中复用。

**接口：**
```csharp
public class PersonnelSearchHelper
{
    private CancellationTokenSource? _debounceTokenSource;
    private const int DebounceDelayMs = 300;
    
    // 执行搜索（带防抖）
    public async Task<List<PersonnelDto>> SearchAsync(
        string query,
        IEnumerable<PersonnelDto> candidates,
        CancellationToken cancellationToken = default);
    
    // 立即搜索（不防抖）
    public List<PersonnelDto> SearchImmediate(
        string query,
        IEnumerable<PersonnelDto> candidates);
}
```

## 数据模型

### MatchResult

```csharp
public class MatchResult
{
    public PersonnelDto Personnel { get; set; }
    public int Score { get; set; }  // 匹配分数，越高越匹配
    public MatchType Type { get; set; }  // 匹配类型
    public int EditDistance { get; set; }  // 编辑距离（用于编辑距离匹配）
    public double Similarity { get; set; }  // 相似度（0-1之间）
}

public enum MatchType
{
    None = 0,                    // 无匹配
    ExactMatch = 1,              // 完全匹配
    PrefixMatch = 2,             // 前缀匹配
    SubstringMatch = 3,          // 子串匹配
    PinyinExactMatch = 4,        // 拼音完全匹配
    PinyinPrefixMatch = 5,       // 拼音前缀匹配
    PinyinInitialsExactMatch = 6,// 拼音首字母完全匹配
    PinyinInitialsPrefixMatch = 7,// 拼音首字母前缀匹配
    EditDistanceMatch = 8,       // 编辑距离模糊匹配
    SequenceFuzzyMatch = 9       // 序列模糊匹配
}
```

### FuzzyMatchOptions

```csharp
public class FuzzyMatchOptions
{
    // 基础选项
    public bool EnableFuzzyMatch { get; set; } = true;
    public bool EnablePinyinMatch { get; set; } = true;
    public bool EnableEditDistanceMatch { get; set; } = true;
    public bool CaseSensitive { get; set; } = false;
    public int MaxResults { get; set; } = 50;
    public int MinScore { get; set; } = 0;
    
    // 编辑距离选项
    public int MaxEditDistance { get; set; } = 2;  // 最大编辑距离
    public bool AutoAdjustEditDistance { get; set; } = true;  // 根据查询长度自动调整
    
    // 拼音选项
    public bool EnableFullPinyinMatch { get; set; } = true;  // 启用完整拼音匹配
    public bool EnablePinyinInitialsMatch { get; set; } = true;  // 启用拼音首字母匹配
    
    // 分数调整选项
    public bool EnableLengthSimilarityBonus { get; set; } = true;  // 启用长度相似度加成
    public bool EnablePositionBonus { get; set; } = true;  // 启用匹配位置加成
    public bool EnableContinuousMatchBonus { get; set; } = true;  // 启用连续匹配加成
}
```


### PersonnelSelectedEventArgs

```csharp
public class PersonnelSelectedEventArgs : EventArgs
{
    public PersonnelDto? Personnel { get; set; }
    public PersonnelDto? PreviousPersonnel { get; set; }
}
```

## 正确性属性

*属性是一个特征或行为，应该在系统的所有有效执行中保持为真——本质上是关于系统应该做什么的正式陈述。属性作为人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1：输入响应一致性
*对于任意*输入文本，系统应该返回过滤后的建议列表，且建议列表中的所有人员都应该匹配输入文本
**验证：需求 1.2**

### 属性 2：模糊匹配完整性
*对于任意*部分匹配的输入文本，系统应该返回所有包含该文本的人员（不区分大小写）
**验证：需求 1.3, 2.1, 2.2**

### 属性 3：拼音匹配正确性
*对于任意*拼音首字母输入，系统应该返回所有姓名拼音首字母匹配的人员
**验证：需求 1.4**

### 属性 4：选择状态一致性
*对于任意*人员的选择操作，系统应该正确设置SelectedPersonnel属性并触发PersonnelSelected事件
**验证：需求 1.5**

### 属性 5：大小写不敏感性
*对于任意*输入文本的大小写变体，系统应该返回相同的匹配结果
**验证：需求 2.1**

### 属性 6：非连续字符匹配
*对于任意*非连续字符序列，如果这些字符在人员姓名中按顺序出现，系统应该返回该人员
**验证：需求 2.3**

### 属性 7：清空恢复一致性
*对于任意*搜索状态，清空搜索文本后应该恢复显示所有可用人员
**验证：需求 2.6**

### 属性 8：匹配度排序正确性
*对于任意*匹配结果集，系统应该按照匹配度排序，完全匹配的结果排在前面，然后是前缀匹配、子串匹配、拼音匹配、模糊匹配
**验证：需求 2.8**

### 属性 9：双向同步一致性
*对于任意*输入文本，AutoSuggestBox的建议列表和人员列表应该同时更新并保持一致
**验证：需求 3.2**

### 属性 10：选择同步正确性
*对于任意*从AutoSuggestBox选择的人员，人员列表中应该高亮显示该人员；反之亦然
**验证：需求 3.3, 3.4**

### 属性 11：验证警告显示
*对于任意*不满足技能要求的人员选择，系统应该显示警告提示但仍允许选择
**验证：需求 3.5**

### 属性 12：属性配置生效性
*对于任意*组件属性配置（占位符文本、最小宽度等），配置应该正确应用到UI
**验证：需求 4.2**

### 属性 13：双向绑定正确性
*对于任意*SelectedPersonnel属性的设置和获取操作，值应该正确传递
**验证：需求 4.3**

### 属性 14：事件触发一致性
*对于任意*选择变化，PersonnelSelected事件应该被触发且包含正确的人员信息
**验证：需求 4.4**

### 属性 15：过滤条件应用正确性
*对于任意*过滤条件，系统应该只显示满足条件的人员
**验证：需求 4.5**

### 属性 16：无障碍属性完整性
*对于任意*建议项，应该包含AutomationProperties.Name属性
**验证：需求 5.3**

### 属性 17：防抖效果正确性
*对于任意*快速连续输入，系统应该减少搜索调用次数（使用防抖技术）
**验证：需求 6.2**

### 属性 18：拼音完全匹配正确性
*对于任意*完整拼音输入，系统应该返回拼音完全匹配的人员
**验证：需求 1.4, 2.1**

### 属性 19：拼音前缀匹配正确性
*对于任意*拼音前缀输入，系统应该返回拼音以该前缀开头的人员
**验证：需求 1.4, 2.2**

### 属性 20：编辑距离容错性
*对于任意*包含少量错误的输入（编辑距离≤阈值），系统应该返回相近的匹配结果
**验证：需求 2.1, 2.3**

### 属性 21：标准化一致性
*对于任意*包含空格或特殊字符的输入，标准化后应该与不含这些字符的输入产生相同的匹配结果
**验证：需求 2.1**

### 属性 22：分数计算单调性
*对于任意*两个匹配结果，更精确的匹配应该获得更高的分数
**验证：需求 2.8**


## 错误处理

### 1. 空数据处理
- 当AvailablePersonnel为null或空时，显示"暂无可用人员"提示
- 搜索结果为空时，显示"未找到匹配的人员"提示

### 2. 异常处理
- 模糊匹配算法异常：降级到简单的Contains匹配
- 拼音转换异常：跳过拼音匹配，只使用其他匹配方式
- 事件触发异常：记录日志但不影响UI

### 3. 性能保护
- 输入防抖：300ms延迟
- 最大结果数限制：50个
- 超时保护：搜索超过200ms自动取消

### 4. 验证错误
- 选择不可用人员：显示警告但允许选择
- 选择技能不匹配人员：显示警告但允许选择

## 测试策略

### 单元测试

使用xUnit框架进行单元测试，覆盖以下场景：

1. **FuzzyMatcher测试**
   - 测试完全匹配
   - 测试前缀匹配
   - 测试子串匹配
   - 测试拼音首字母匹配
   - 测试大小写不敏感
   - 测试空输入
   - 测试无匹配结果

2. **PersonnelSelectorViewModel测试**
   - 测试UpdateSuggestions方法
   - 测试SelectPersonnel方法
   - 测试ClearSelection方法
   - 测试属性变化通知

3. **PersonnelSelectorControl测试**
   - 测试依赖属性设置和获取
   - 测试事件触发
   - 测试双向绑定

### 属性基础测试（Property-Based Testing）

使用FsCheck库进行属性基础测试，每个测试运行至少100次迭代：

1. **属性 1-17 的测试**
   - 为每个正确性属性编写对应的属性测试
   - 使用随机生成的输入数据
   - 验证属性在所有情况下都成立

2. **测试数据生成器**
   - PersonnelDto生成器：生成随机人员数据
   - 搜索文本生成器：生成各种搜索文本（中文、拼音、混合）
   - 过滤条件生成器：生成各种过滤条件

### 集成测试

1. **EditShiftAssignmentDialog集成测试**
   - 测试对话框打开时的初始化
   - 测试搜索框和列表的双向同步
   - 测试人员选择流程

2. **ScheduleResultPage集成测试**
   - 测试筛选工具栏的人员搜索
   - 测试搜索结果的应用

### UI自动化测试

使用WinAppDriver进行UI自动化测试：

1. 测试键盘导航
2. 测试鼠标交互
3. 测试无障碍功能
4. 测试响应式布局

## 实现细节

### 增强的模糊匹配算法

基于"标准化处理→拼音映射→模糊比对→结果排序"四步流程，实现高精度的拼音+模糊匹配：

#### 第一步：标准化处理

**目的**：消除格式干扰，确保匹配准确性

- **文本标准化**：
  - 转小写（不区分大小写）
  - 去除空格和特殊字符（如！、，、。等）
  - 示例："苹果！" → "苹果"，" 计算 机 " → "计算机"

- **查询词标准化**：
  - 转小写
  - 去除空格
  - 过滤非字母非中文字符
  - 示例："Ping Guo123" → "pingguo"，"PYQ " → "pyq"

#### 第二步：拼音映射

**目的**：建立汉字与拼音的对应关系，支持全拼和简拼双模式

- **全拼生成**：将汉字转为连续无空格全拼
  - 示例："朋友圈" → "pengyouquan"
  
- **简拼生成**：提取每个汉字拼音的首字母
  - 示例："朋友圈" → "pyq"

- **拼音缓存**：预生成并缓存拼音，避免重复转换

#### 第三步：多层匹配策略

使用分层匹配，按优先级和精度计算分数：

1. **完全匹配**（基础分：100）
   - 输入文本与人员姓名完全相同（不区分大小写）
   - 示例："张三" 完全匹配 "张三"

2. **前缀匹配**（基础分：90）
   - 人员姓名以输入文本开头
   - 示例："张" 前缀匹配 "张三"

3. **子串匹配**（基础分：70）
   - 人员姓名包含输入文本
   - 示例："三" 子串匹配 "张三"

4. **拼音完全匹配**（基础分：85）
   - 输入文本与完整拼音完全匹配
   - 示例："zhangsan" 完全匹配 "张三"（zhang san）

5. **拼音前缀匹配**（基础分：75）
   - 完整拼音以输入文本开头
   - 示例："zhang" 前缀匹配 "张三"（zhang san）

6. **拼音首字母完全匹配**（基础分：65）
   - 输入文本与拼音首字母完全匹配
   - 示例："zs" 完全匹配 "张三"（zhang san → zs）

7. **拼音首字母前缀匹配**（基础分：55）
   - 拼音首字母以输入文本开头
   - 示例："z" 前缀匹配 "张三"（zs）

8. **编辑距离模糊匹配**（基础分：30-50，根据编辑距离动态计算）
   - 使用Levenshtein编辑距离算法
   - 允许少量错误（替换、插入、删除）
   - 分数计算：`50 - (编辑距离 * 10)`
   - 阈值：查询词长度≤3时，最大编辑距离=1；长度≥4时，最大编辑距离=2
   - 示例："zhangsa" 匹配 "zhangsan"（编辑距离1，分数40）

9. **序列模糊匹配**（基础分：25）
   - 输入文本的字符在人员姓名中按顺序出现
   - 示例："张四" 匹配 "张三四"

#### 第四步：精细化分数计算

在基础分数上，根据匹配精度进行微调：

- **长度相似度加成**：`基础分 + (1 - |查询长度 - 匹配长度| / max(查询长度, 匹配长度)) * 5`
- **匹配位置加成**：匹配位置越靠前，加成越高（最多+3分）
- **连续匹配加成**：连续匹配的字符越多，加成越高（最多+2分）

#### 第五步：结果排序

- 按匹配分数降序排序
- 分数相同时，按姓名字典序排序
- 限制最大返回结果数（默认50个）

### 拼音转换

使用TinyPinyin.NET库进行拼音转换，支持完整拼音和拼音首字母：

```csharp
// 获取拼音首字母
public static string GetPinyinInitials(string chinese)
{
    var result = new StringBuilder();
    foreach (char c in chinese)
    {
        if (TinyPinyinLib.IsChinese(c))
        {
            var pinyin = TinyPinyinLib.GetPinyin(c);
            if (!string.IsNullOrEmpty(pinyin))
            {
                result.Append(char.ToLower(pinyin[0]));
            }
        }
        else if (char.IsLetterOrDigit(c))
        {
            result.Append(char.ToLower(c));
        }
    }
    return result.ToString();
}

// 获取完整拼音（无空格）
public static string GetFullPinyin(string chinese)
{
    var result = new StringBuilder();
    foreach (char c in chinese)
    {
        if (TinyPinyinLib.IsChinese(c))
        {
            var pinyin = TinyPinyinLib.GetPinyin(c);
            if (!string.IsNullOrEmpty(pinyin))
            {
                result.Append(pinyin.ToLower());
            }
        }
        else if (char.IsLetterOrDigit(c))
        {
            result.Append(char.ToLower(c));
        }
    }
    return result.ToString();
}
```

### 编辑距离算法

使用Levenshtein编辑距离算法计算两个字符串的相似度：

```csharp
private static int CalculateEditDistance(string source, string target)
{
    if (string.IsNullOrEmpty(source))
        return target?.Length ?? 0;
    if (string.IsNullOrEmpty(target))
        return source.Length;

    int[,] distance = new int[source.Length + 1, target.Length + 1];

    // 初始化第一行和第一列
    for (int i = 0; i <= source.Length; i++)
        distance[i, 0] = i;
    for (int j = 0; j <= target.Length; j++)
        distance[0, j] = j;

    // 动态规划计算编辑距离
    for (int i = 1; i <= source.Length; i++)
    {
        for (int j = 1; j <= target.Length; j++)
        {
            int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;
            distance[i, j] = Math.Min(
                Math.Min(distance[i - 1, j] + 1,      // 删除
                         distance[i, j - 1] + 1),     // 插入
                         distance[i - 1, j - 1] + cost); // 替换
        }
    }

    return distance[source.Length, target.Length];
}
```

### 标准化处理

统一文本格式，消除干扰字符：

```csharp
private static string NormalizeText(string text, bool removePunctuation = true)
{
    if (string.IsNullOrWhiteSpace(text))
        return string.Empty;

    var result = new StringBuilder();
    foreach (char c in text)
    {
        if (char.IsWhiteSpace(c))
            continue;  // 去除空格
        
        if (removePunctuation && char.IsPunctuation(c))
            continue;  // 去除标点符号
        
        result.Append(char.ToLower(c));  // 转小写
    }
    
    return result.ToString();
}
```

### 防抖实现

使用CancellationTokenSource实现防抖：

```csharp
private CancellationTokenSource? _debounceTokenSource;
private const int DebounceDelayMs = 300;

public async void OnTextChanged(string text)
{
    _debounceTokenSource?.Cancel();
    _debounceTokenSource = new CancellationTokenSource();
    
    try
    {
        await Task.Delay(DebounceDelayMs, _debounceTokenSource.Token);
        UpdateSuggestions(text);
    }
    catch (TaskCanceledException)
    {
        // 被取消，忽略
    }
}
```

### 虚拟化

使用ItemsRepeater或ListView的虚拟化功能：

```xml
<ListView ItemsSource="{x:Bind Suggestions}"
          VirtualizingStackPanel.VirtualizationMode="Recycling">
</ListView>
```

## 性能考虑

1. **搜索优化**
   - 使用并行查询（PLINQ）处理大量数据
   - 缓存拼音转换结果
   - 限制最大结果数

2. **UI优化**
   - 使用虚拟化减少DOM元素
   - 使用防抖减少搜索频率
   - 异步加载人员数据

3. **内存优化**
   - 及时释放CancellationTokenSource
   - 避免保存大量历史搜索结果
   - 使用弱引用缓存

## 无障碍支持

1. **AutomationProperties设置**
   ```xml
   <AutoSuggestBox AutomationProperties.Name="人员选择器"
                   AutomationProperties.HelpText="输入姓名搜索或从下拉列表选择人员"
                   AutomationProperties.LiveSetting="Polite"/>
   ```

2. **键盘导航**
   - Tab键：在控件间导航
   - 上下箭头：在建议列表中导航
   - 回车键：选择当前项
   - Esc键：关闭建议列表

3. **屏幕阅读器支持**
   - 为每个建议项设置AutomationProperties.Name
   - 使用LiveRegion通知状态变化
   - 提供清晰的焦点指示

## 依赖项

1. **NuGet包**
   - TinyPinyin.NET：拼音转换
   - FsCheck：属性基础测试
   - xUnit：单元测试框架

2. **系统依赖**
   - .NET 8.0
   - WinUI 3
   - CommunityToolkit.Mvvm

## 向后兼容性

1. 保持现有的AutoSuggestBox实现不变
2. 新的PersonnelSelectorControl作为可选组件
3. 逐步迁移现有页面到新控件
4. 提供迁移指南和示例代码


## WinUI 3 现有控件分析

### AutoSuggestBox

WinUI 3提供的AutoSuggestBox已经满足大部分需求：

**内置功能：**
- ✅ 文本输入
- ✅ 下拉建议列表
- ✅ ItemTemplate自定义显示
- ✅ TextChanged、SuggestionChosen、QuerySubmitted事件
- ✅ 键盘导航（上下箭头、回车、Esc）
- ✅ 无障碍支持（AutomationProperties）

**需要自己实现的功能：**
- ❌ 模糊匹配算法（需要自定义）
- ❌ 拼音首字母匹配（需要自定义）
- ❌ 匹配度排序（需要自定义）
- ❌ 防抖逻辑（需要自定义）
- ❌ 点击展开显示所有项（需要手动触发）

### ComboBox

WinUI 3的ComboBox也可以考虑：

**内置功能：**
- ✅ 下拉选择
- ✅ IsEditable属性（可编辑模式）
- ✅ 键盘导航
- ✅ 无障碍支持

**局限性：**
- ❌ 可编辑模式下的搜索功能较弱
- ❌ 不支持自定义搜索算法
- ❌ 建议列表显示不如AutoSuggestBox灵活

### 结论

**推荐使用AutoSuggestBox作为基础控件**，原因：

1. **更适合搜索场景**：AutoSuggestBox专为搜索设计，用户体验更好
2. **灵活性高**：可以完全控制建议列表的生成和显示
3. **已有实现**：项目中已经在使用，迁移成本低
4. **社区支持**：WinUI 3社区推荐用于搜索场景

### 实现策略

不需要创建全新的自定义控件，而是：

1. **创建辅助类**：FuzzyMatcher、PinyinHelper
2. **创建ViewModel基类**：PersonnelSelectorViewModelBase
3. **提供代码模板**：标准化的AutoSuggestBox使用模式
4. **可选：创建Behavior**：使用Microsoft.Xaml.Behaviors.WinUI封装可复用逻辑

这种方式的优势：
- 利用WinUI 3原生控件的稳定性和性能
- 减少自定义控件的维护成本
- 更容易集成到现有代码中
- 保持与WinUI 3设计语言的一致性
