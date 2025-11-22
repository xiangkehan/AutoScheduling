# 需求文档

## 简介

改进测试数据生成器的数据生成顺序和逻辑，确保生成的哨位都有足够的符合技能要求的可用人员。当前的生成器先为人员随机分配技能，再生成哨位，这可能导致某些哨位找不到符合技能要求的人员。新的方案将先生成人员（不分配技能）和哨位（定义技能需求），然后根据哨位的技能需求智能地为人员分配技能，确保每个哨位都有足够的可用人员。

## 术语表

- **TestDataGenerator**: 测试数据生成器主类，协调各个生成器完成测试数据的生成
- **PersonnelGenerator**: 人员数据生成器，负责生成人员基础信息
- **PositionGenerator**: 哨位数据生成器，负责生成哨位信息和技能需求
- **SkillAssigner**: 技能分配器（新增），负责根据哨位需求为人员分配技能
- **PersonnelDto**: 人员数据传输对象，包含人员的所有信息
- **PositionDto**: 哨位数据传输对象，包含哨位的所有信息
- **SkillDto**: 技能数据传输对象，包含技能的基本信息

## 需求

### 需求 1：调整数据生成顺序

**用户故事：** 作为开发人员，我希望测试数据生成器按照合理的顺序生成数据，以便生成的数据更加真实和可用

#### 验收标准

1. WHEN TestDataGenerator 执行 GenerateTestData 方法时，THE TestDataGenerator SHALL 按照以下顺序生成数据：技能 → 人员（无技能）→ 哨位（含技能需求）→ 技能分配 → 其他数据
2. WHEN PersonnelGenerator 生成人员数据时，THE PersonnelGenerator SHALL 不分配任何技能，SkillIds 和 SkillNames 列表为空
3. WHEN PositionGenerator 生成哨位数据时，THE PositionGenerator SHALL 为每个哨位分配 1-2 个所需技能
4. WHEN 技能分配完成后，THE TestDataGenerator SHALL 更新哨位的 AvailablePersonnelIds 和 AvailablePersonnelNames 列表

### 需求 2：创建智能技能分配器

**用户故事：** 作为开发人员，我希望有一个智能的技能分配器，能够根据哨位的技能需求为人员分配技能，确保每个哨位都有足够的可用人员，并创建多技能人员以提高人员复用率

#### 验收标准

1. THE TestDataGenerator SHALL 创建一个新的 SkillAssigner 类，负责根据哨位需求为人员分配技能
2. WHEN SkillAssigner 执行技能分配时，THE SkillAssigner SHALL 确保每个哨位至少有配置数量（MinPersonnelPerPosition）的可用人员符合其技能要求
3. WHEN SkillAssigner 为人员分配技能时，THE SkillAssigner SHALL 优先为需求人员较少的哨位分配人员
4. WHEN SkillAssigner 为人员分配技能时，THE SkillAssigner SHALL 为每个人员分配 1-3 个技能
5. WHEN SkillAssigner 分析哨位技能需求时，THE SkillAssigner SHALL 识别高频共现的技能组合
6. WHEN SkillAssigner 创建多技能人员时，THE SkillAssigner SHALL 为 30-40% 的可用人员添加额外的共现技能
7. WHEN SkillAssigner 为人员添加额外技能时，THE SkillAssigner SHALL 确保人员技能数量不超过 3 个
8. WHEN SkillAssigner 完成技能分配后，THE SkillAssigner SHALL 返回更新后的人员列表

### 需求 3：更新 PersonnelGenerator

**用户故事：** 作为开发人员，我希望 PersonnelGenerator 只生成人员的基础信息，不分配技能，以便后续根据哨位需求进行智能分配

#### 验收标准

1. WHEN PersonnelGenerator 生成人员数据时，THE PersonnelGenerator SHALL 不接收技能列表作为依赖项
2. WHEN PersonnelGenerator 生成人员数据时，THE PersonnelGenerator SHALL 将 SkillIds 设置为空列表
3. WHEN PersonnelGenerator 生成人员数据时，THE PersonnelGenerator SHALL 将 SkillNames 设置为空列表
4. WHEN PersonnelGenerator 生成人员数据时，THE PersonnelGenerator SHALL 保留其他所有字段的生成逻辑不变

### 需求 4：更新 PositionGenerator

**用户故事：** 作为开发人员，我希望 PositionGenerator 生成哨位时不计算可用人员列表，因为此时人员还没有技能

#### 验收标准

1. WHEN PositionGenerator 生成哨位数据时，THE PositionGenerator SHALL 不接收人员列表作为依赖项
2. WHEN PositionGenerator 生成哨位数据时，THE PositionGenerator SHALL 将 AvailablePersonnelIds 设置为空列表
3. WHEN PositionGenerator 生成哨位数据时，THE PositionGenerator SHALL 将 AvailablePersonnelNames 设置为空列表
4. WHEN PositionGenerator 生成哨位数据时，THE PositionGenerator SHALL 保留技能需求的生成逻辑不变

### 需求 5：添加配置参数

**用户故事：** 作为开发人员，我希望能够配置每个哨位的最小可用人员数量、人员可用率和退役率，以便控制测试数据的质量和适应不同的测试场景

#### 验收标准

1. THE TestDataConfiguration SHALL 添加一个新的配置属性 MinPersonnelPerPosition，默认值为 3
2. THE TestDataConfiguration SHALL 添加一个新的配置属性 PersonnelAvailabilityRate，默认值为 0.85
3. THE TestDataConfiguration SHALL 添加一个新的配置属性 PersonnelRetirementRate，默认值为 0.10
4. WHEN TestDataConfiguration 验证配置时，THE TestDataConfiguration SHALL 确保 MinPersonnelPerPosition 大于 0 且不超过 PersonnelCount
5. WHEN TestDataConfiguration 验证配置时，THE TestDataConfiguration SHALL 确保 PersonnelAvailabilityRate 在 0.0-1.0 之间
6. WHEN TestDataConfiguration 验证配置时，THE TestDataConfiguration SHALL 确保 PersonnelRetirementRate 在 0.0-1.0 之间
7. WHEN TestDataConfiguration 验证配置时，THE TestDataConfiguration SHALL 确保 PersonnelAvailabilityRate 和 PersonnelRetirementRate 之和不超过 1.0
8. WHEN SkillAssigner 分配技能时，THE SkillAssigner SHALL 使用 MinPersonnelPerPosition 作为每个哨位的最小可用人员数量
9. WHEN PersonnelGenerator 生成人员时，THE PersonnelGenerator SHALL 使用 PersonnelAvailabilityRate 确定人员的 IsAvailable 状态
10. WHEN PersonnelGenerator 生成人员时，THE PersonnelGenerator SHALL 使用 PersonnelRetirementRate 确定人员的 IsRetired 状态
