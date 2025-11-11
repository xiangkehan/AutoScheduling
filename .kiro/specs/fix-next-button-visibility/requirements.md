# Requirements Document

## Introduction

本功能旨在修复创建排班页面中"下一步"按钮的可见性问题。当前实现中，"下一步"按钮在第5步（最后一步）才显示，但实际上应该在步骤1-4显示，在第5步隐藏。这导致用户无法从第1步前进到后续步骤。

## Glossary

- **CreateSchedulingPage**: 创建排班页面，用户通过分步向导配置排班参数的界面
- **NextStepCommand**: 下一步命令，用于从当前步骤前进到下一个步骤
- **CurrentStep**: 当前步骤编号，范围从1到5
- **Visibility Converter**: 可见性转换器，根据条件控制UI元素的显示/隐藏

## Requirements

### Requirement 1

**User Story:** 作为排班管理员，我希望在创建排班的步骤1-4中看到"下一步"按钮，以便我可以逐步完成排班配置流程

#### Acceptance Criteria

1. WHEN CurrentStep等于1, THE CreateSchedulingPage SHALL显示"下一步"按钮
2. WHEN CurrentStep等于2, THE CreateSchedulingPage SHALL显示"下一步"按钮
3. WHEN CurrentStep等于3, THE CreateSchedulingPage SHALL显示"下一步"按钮
4. WHEN CurrentStep等于4, THE CreateSchedulingPage SHALL显示"下一步"按钮
5. WHEN CurrentStep等于5, THE CreateSchedulingPage SHALL隐藏"下一步"按钮

### Requirement 2

**User Story:** 作为排班管理员，我希望在最后一步（第5步）看到"开始排班"和"保存为模板"按钮，而不是"下一步"按钮，以便我可以完成或保存排班配置

#### Acceptance Criteria

1. WHEN CurrentStep等于5, THE CreateSchedulingPage SHALL显示"开始排班"按钮
2. WHEN CurrentStep等于5, THE CreateSchedulingPage SHALL显示"保存为模板"按钮
3. WHEN CurrentStep小于5, THE CreateSchedulingPage SHALL隐藏"开始排班"按钮
4. WHEN CurrentStep小于5, THE CreateSchedulingPage SHALL隐藏"保存为模板"按钮

### Requirement 3

**User Story:** 作为排班管理员，我希望"上一步"按钮在步骤2-5中可见，在步骤1中隐藏，以便我可以返回修改之前的配置

#### Acceptance Criteria

1. WHEN CurrentStep等于1, THE CreateSchedulingPage SHALL隐藏"上一步"按钮
2. WHEN CurrentStep大于1, THE CreateSchedulingPage SHALL显示"上一步"按钮
