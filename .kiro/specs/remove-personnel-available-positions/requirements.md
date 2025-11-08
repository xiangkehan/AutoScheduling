# Requirements Document

## Introduction

本需求文档描述了从人员管理功能中移除"可用哨位"相关代码的需求。由于人员数据模型（Personal）中不包含可用哨位集合字段，但 DTO 和 UI 层仍保留了相关代码，导致数据结构不一致。本功能将清理这些冗余代码，确保人员创建和编辑流程只关注实际存在的数据字段。

## Glossary

- **Personnel System**: 人员管理系统，负责管理人员的基本信息、技能和状态
- **Personal Model**: 人员数据模型，定义在 Models/Personal.cs 中
- **PersonnelDto**: 人员数据传输对象，用于在服务层和 UI 层之间传递数据
- **UI Layer**: 用户界面层，包括 XAML 和 code-behind 文件
- **Available Positions**: 可用哨位，指人员可以被分配到的哨位列表（当前不在数据模型中）

## Requirements

### Requirement 1

**User Story:** 作为开发人员，我希望人员 DTO 与数据模型保持一致，以便数据结构清晰且易于维护

#### Acceptance Criteria

1. WHEN PersonnelDto 被定义时，THE Personnel System SHALL NOT include AvailablePositionIds property
2. WHEN PersonnelDto 被定义时，THE Personnel System SHALL NOT include AvailablePositionNames property
3. WHEN CreatePersonnelDto 被定义时，THE Personnel System SHALL NOT include AvailablePositionIds property
4. WHEN UpdatePersonnelDto 被定义时，THE Personnel System SHALL NOT include AvailablePositionIds property

### Requirement 2

**User Story:** 作为用户，我希望在创建人员时只需要填写实际需要的信息，以便操作更简洁高效

#### Acceptance Criteria

1. WHEN user creates a new personnel，THE Personnel System SHALL NOT display available positions selection control
2. WHEN user creates a new personnel，THE Personnel System SHALL require name input
3. WHEN user creates a new personnel，THE Personnel System SHALL require at least one skill selection
4. WHEN user creates a new personnel，THE Personnel System SHALL allow availability status configuration

### Requirement 3

**User Story:** 作为用户，我希望在编辑人员时只能修改实际存在的字段，以便避免数据不一致

#### Acceptance Criteria

1. WHEN user edits an existing personnel，THE Personnel System SHALL NOT display available positions selection control
2. WHEN user edits an existing personnel，THE Personnel System SHALL allow modification of name
3. WHEN user edits an existing personnel，THE Personnel System SHALL allow modification of skills
4. WHEN user edits an existing personnel，THE Personnel System SHALL allow modification of availability and retirement status

### Requirement 4

**User Story:** 作为用户，我希望在查看人员详情时只看到实际存在的信息，以便信息展示清晰准确

#### Acceptance Criteria

1. WHEN user views personnel details，THE Personnel System SHALL NOT display available positions information
2. WHEN user views personnel details，THE Personnel System SHALL display personnel name
3. WHEN user views personnel details，THE Personnel System SHALL display personnel skills
4. WHEN user views personnel details，THE Personnel System SHALL display personnel status (available/retired)

### Requirement 5

**User Story:** 作为开发人员，我希望人员服务层代码不处理可用哨位相关逻辑，以便代码逻辑与数据模型一致

#### Acceptance Criteria

1. WHEN PersonnelService maps between model and DTO，THE Personnel System SHALL NOT process AvailablePositionIds
2. WHEN PersonnelService creates personnel，THE Personnel System SHALL NOT validate AvailablePositionIds
3. WHEN PersonnelService updates personnel，THE Personnel System SHALL NOT update AvailablePositionIds
4. WHEN PersonnelRepository queries personnel data，THE Personnel System SHALL NOT include available positions in query results
