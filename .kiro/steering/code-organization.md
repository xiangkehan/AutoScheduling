# 代码组织规范

## 文件大小限制

- **单个文件最大行数**: 不超过 1000 行
- **推荐行数**: 300-600 行为最佳
- **超过限制时**: 必须拆分为多个文件

## 文件拆分策略

### 当文件过大时，按以下原则拆分：

1. **ViewModel 拆分**
   - 将大型 ViewModel 拆分为多个部分类（partial class）
   - 按功能模块分离：`{Name}ViewModel.cs`（主文件）、`{Name}ViewModel.Commands.cs`（命令）、`{Name}ViewModel.Validation.cs`（验证逻辑）
   - 或按业务领域拆分为独立的 ViewModel

2. **Service 拆分**
   - 将复杂 Service 拆分为多个专门的 Service
   - 使用组合模式而非继承
   - 例如：`PersonnelService` → `PersonnelQueryService` + `PersonnelCommandService`

3. **Repository 拆分**
   - 按实体或聚合根分离
   - 复杂查询逻辑可提取到独立的 Query 类

4. **View 拆分**
   - 将大型 XAML 页面拆分为多个 UserControl
   - 每个 UserControl 负责一个独立的 UI 区域
   - 使用 ContentControl 或 Frame 进行组合

## 设计阶段要求

### 在实现任何功能前，必须提供：

1. **文件结构设计**
   ```
   改动的文件列表：
   - 新增文件：路径 + 简要说明 + 预估行数
   - 修改文件：路径 + 改动内容 + 改动后预估行数
   - 删除文件：路径 + 删除原因
   ```

2. **目录结构合理性检查**
   - 确保新文件放在正确的目录下
   - 遵循现有的目录组织规范（参考 structure.md）
   - 如需新增目录，说明原因和用途

3. **依赖关系说明**
   - 新文件的依赖关系
   - 对现有文件的影响范围
   - 是否需要更新 DI 注册

## 代码组织最佳实践

### 类的职责单一性

- 每个类只负责一个明确的职责
- 避免"上帝类"（God Class）
- 使用接口隔离原则（ISP）

### 方法长度控制

- 单个方法不超过 50 行
- 复杂逻辑提取为私有方法
- 使用有意义的方法名

### 命名空间组织

- 按功能模块组织命名空间
- 避免过深的命名空间层级（不超过 4 层）
- 命名空间与目录结构保持一致

### 部分类（Partial Class）使用规范

适合使用 partial class 的场景：
- ViewModel 的命令和属性分离
- 自动生成代码与手写代码分离
- 同一类的不同关注点分离

不适合使用 partial class 的场景：
- 不同职责的逻辑（应拆分为独立类）
- 跨文件的复杂依赖（难以维护）

## 重构触发条件

当出现以下情况时，必须重构：

1. 文件超过 800 行（警告）或 1000 行（强制）
2. 类的公共方法超过 20 个
3. 方法的圈复杂度超过 10
4. 类的依赖超过 7 个
5. 代码重复率超过 10%

## 示例：大型 ViewModel 拆分

### 拆分前（1200 行）
```
SchedulingViewModel.cs (1200 行)
```

### 拆分后
```
ViewModels/Scheduling/
├── SchedulingViewModel.cs (300 行 - 主逻辑和属性)
├── SchedulingViewModel.Commands.cs (200 行 - 命令定义)
├── SchedulingViewModel.Validation.cs (150 行 - 验证逻辑)
└── SchedulingViewModel.Helpers.cs (150 行 - 辅助方法)
```

或按功能拆分：
```
ViewModels/Scheduling/
├── SchedulingViewModel.cs (200 行 - 主协调逻辑)
├── PersonnelSelectionViewModel.cs (300 行 - 人员选择)
├── PositionConfigViewModel.cs (250 行 - 哨位配置)
└── ConstraintConfigViewModel.cs (300 行 - 约束配置)
```

## 检查清单

在提交代码前，确认：

- [ ] 所有文件行数 ≤ 1000 行
- [ ] 每个类职责单一明确
- [ ] 文件放置在正确的目录下
- [ ] 命名空间与目录结构一致
- [ ] 已更新相关的 DI 注册
- [ ] 已提供文件结构设计文档
