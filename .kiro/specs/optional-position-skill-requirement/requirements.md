# 需求文档

## 简介

本功能允许哨位对技能没有要求。在创建或编辑哨位时，用户可以选择不指定任何技能要求，或者主动选择"无技能要求"选项。这使得系统更加灵活，能够支持不需要特定技能的哨位。

## 术语表

- **System**: AutoScheduling3 应用程序
- **Position**: 哨位，表示一个需要人员值守的岗位
- **Skill**: 技能，表示人员具备的能力或资质
- **Skill Requirement**: 技能要求，哨位对人员技能的要求
- **User**: 用户，使用系统创建和管理哨位的人员
- **UI**: 用户界面，用户与系统交互的界面

## 需求

### 需求 1

**用户故事：** 作为系统管理员，我希望能够创建不需要任何技能要求的哨位，以便支持不需要特定技能的岗位。

#### 验收标准

1. WHEN User creates a new Position, THE System SHALL allow User to submit the Position without selecting any Skill
2. WHEN User creates a Position without selecting any Skill, THE System SHALL store the Position with an empty Skill Requirement list
3. WHEN User views a Position with no Skill Requirement, THE System SHALL display an indicator showing "无技能要求" or equivalent text
4. WHEN User edits an existing Position, THE System SHALL allow User to remove all Skill Requirements

### 需求 2

**用户故事：** 作为系统管理员，我希望在创建哨位时能够明确选择"无技能要求"选项，以便清楚地表达该哨位不需要技能。

#### 验收标准

1. WHEN User opens the Position creation UI, THE System SHALL display an option to explicitly select "无技能要求"
2. WHEN User selects the "无技能要求" option, THE System SHALL disable or clear any previously selected Skills
3. WHEN User selects one or more Skills after selecting "无技能要求", THE System SHALL automatically deselect the "无技能要求" option
4. WHEN User submits a Position with "无技能要求" selected, THE System SHALL store the Position with an empty Skill Requirement list

### 需求 3

**用户故事：** 作为系统用户，我希望系统能够正确处理没有技能要求的哨位，以便在排班时能够将任何人员分配到该哨位。

#### 验收标准

1. WHEN System calculates available personnel for a Position with no Skill Requirement, THE System SHALL include all active personnel in the available personnel list
2. WHEN System performs scheduling for a Position with no Skill Requirement, THE System SHALL consider all active personnel as eligible candidates
3. WHEN System validates a manual assignment to a Position with no Skill Requirement, THE System SHALL accept any active personnel
4. WHEN ConstraintValidator validates skill match for a Position with empty or null RequiredSkillIds, THE System SHALL return true to allow any personnel

### 需求 4

**用户故事：** 作为开发人员，我希望数据模型和验证规则能够支持可选的技能要求，以便系统能够正确存储和处理这类数据。

#### 验收标准

1. THE System SHALL remove the Required validation attribute from the RequiredSkillIds field in CreatePositionDto
2. THE System SHALL remove the MinLength validation attribute from the RequiredSkillIds field in CreatePositionDto
3. THE System SHALL remove the Required validation attribute from the RequiredSkillIds field in UpdatePositionDto
4. THE System SHALL remove the MinLength validation attribute from the RequiredSkillIds field in UpdatePositionDto
5. WHEN System receives a Position creation or update request with an empty RequiredSkillIds list, THE System SHALL accept and process the request without validation errors

### 需求 5

**用户故事：** 作为开发人员，我希望UI层的验证逻辑能够支持可选的技能要求，以便用户能够成功创建和编辑没有技能要求的哨位。

#### 验收标准

1. THE System SHALL remove the skill requirement validation check from PositionViewModel CreatePositionAsync method
2. WHEN User submits a Position creation form with no skills selected, THE System SHALL proceed with the creation without showing error message
3. WHEN User submits a Position update form with no skills selected, THE System SHALL proceed with the update without showing error message
4. WHEN UI displays a Position with no Skill Requirement, THE System SHALL show "无技能要求" text instead of an empty skill list
