# 设计文档

## 概述

本设计文档描述了排班结果展示页面（ScheduleResultPage）的完善方案。该页面将提供四种视图模式、丰富的交互功能和详细的统计信息，帮助用户从多个角度查看和分析排班结果。

## UI 布局设计（ASCII 图）

### 主页面布局

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│  顶部工具栏                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ [<返回] 排班结果：2025年1月排班  (2025-01-01 至 2025-01-31)                      │  │
│  │                                                                                    │  │
│  │ [导出▼] [冲突] [全屏] [比较]    视图: ○Grid ○List ○Personnel ●Position          │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│  筛选工具栏                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ 日期: [2025-01-01] 至 [2025-01-31]  哨位: [全部▼]  人员: [搜索...]  [应用]    │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│  主内容区（根据选择的视图模式显示不同内容）                                              │
│                                                                                          │
│  [视图内容区域 - 见下方各视图详细布局]                                                  │
│                                                                                          │
│                                                                                          │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│  底部操作栏                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │                                    [返回] [重新排班] [确认排班]                   │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```


### Grid View（网格视图）布局

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│  Grid View - 排班表格                                                                    │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │        │ 哨位1    │ 哨位2    │ 哨位3    │ 哨位4    │ 哨位5    │ ...              │  │
│  ├────────┼──────────┼──────────┼──────────┼──────────┼──────────┼──────────────────┤  │
│  │01-01   │          │          │          │          │          │                  │  │
│  │08:00   │  张三    │  李四    │  王五    │  赵六    │  孙七    │ ...              │  │
│  ├────────┼──────────┼──────────┼──────────┼──────────┼──────────┼──────────────────┤  │
│  │01-01   │          │          │          │          │          │                  │  │
│  │10:00   │  周八    │  吴九    │  郑十    │  张三    │  李四    │ ...              │  │
│  ├────────┼──────────┼──────────┼──────────┼──────────┼──────────┼──────────────────┤  │
│  │01-01   │          │          │          │          │          │                  │  │
│  │12:00   │  王五    │  赵六    │  孙七    │  周八    │  吴九    │ ...              │  │
│  ├────────┼──────────┼──────────┼──────────┼──────────┼──────────┼──────────────────┤  │
│  │  ...   │   ...    │   ...    │   ...    │   ...    │   ...    │ ...              │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                          │
│  说明：                                                                                  │
│  • 行头固定，显示日期+时段                                                               │
│  • 列头固定，显示哨位名称                                                                │
│  • 单元格显示人员姓名                                                                    │
│  • 手动指定的单元格有蓝色边框                                                            │
│  • 有冲突的单元格有红色边框                                                              │
│  • 鼠标悬停显示详细信息                                                                  │
│  • 双击单元格可编辑                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```


### Position View（哨位视图）布局

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│  Position View - 按哨位查看                                                              │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ 选择哨位: [哨位1 ▼]                          周次: [第1周 ▼] (2025-01-01 ~ 01-07) │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                          │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ 哨位1 排班表                                                                       │  │
│  ├────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┤  │
│  │ 时段   │  周一   │  周二   │  周三   │  周四   │  周五   │  周六   │  周日   │  │
│  │        │ 01-01   │ 01-02   │ 01-03   │ 01-04   │ 01-05   │ 01-06   │ 01-07   │  │
│  ├────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤  │
│  │ 00:00  │  张三   │  李四   │  王五   │  赵六   │  孙七   │  周八   │  吴九   │  │
│  │ 02:00  │         │         │         │         │         │         │         │  │
│  ├────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤  │
│  │ 02:00  │  郑十   │  张三   │  李四   │  王五   │  赵六   │  孙七   │  周八   │  │
│  │ 04:00  │         │         │         │         │         │         │         │  │
│  ├────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤  │
│  │ 04:00  │  吴九   │  郑十   │  张三   │  李四   │  王五   │  赵六   │  孙七   │  │
│  │ 06:00  │         │         │         │         │         │         │         │  │
│  ├────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤  │
│  │ 06:00  │  周八   │  吴九   │  郑十   │  张三   │  李四   │  王五   │  赵六   │  │
│  │ 08:00  │         │         │         │         │         │         │         │  │
│  ├────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤  │
│  │  ...   │   ...   │   ...   │   ...   │   ...   │   ...   │   ...   │   ...   │  │
│  └────────┴─────────┴─────────┴─────────┴─────────┴─────────┴─────────┴─────────┘  │
│                                                                                          │
│  说明：                                                                                  │
│  • 每个哨位一张独立的表格                                                                │
│  • 行是时段（12个时段，每个时段2小时）                                                   │
│  • 列是星期（周一到周日）                                                                │
│  • 如果排班跨越多周，提供周次切换器                                                      │
│  • 单元格显示人员姓名                                                                    │
│  • 支持打印和导出单个哨位的排班表                                                        │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```


### List View（列表视图）布局

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│  List View - 班次列表                                                                    │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ 排序: [日期▼]  分组: [无分组▼]                                                    │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                          │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ 2025-01-01 08:00-10:00  │  哨位1  │  张三  │  [详情] [编辑]              │   │  │
│  │ └────────────────────────────────────────────────────────────────────────────┘   │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ 2025-01-01 08:00-10:00  │  哨位2  │  李四  │  [详情] [编辑]              │   │  │
│  │ └────────────────────────────────────────────────────────────────────────────┘   │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ 2025-01-01 08:00-10:00  │  哨位3  │  王五  │  [详情] [编辑]  ⚠冲突      │   │  │
│  │ └────────────────────────────────────────────────────────────────────────────┘   │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ 2025-01-01 10:00-12:00  │  哨位1  │  周八  │  [详情] [编辑]              │   │  │
│  │ └────────────────────────────────────────────────────────────────────────────┘   │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ 2025-01-01 10:00-12:00  │  哨位2  │  吴九  │  [详情] [编辑]  🔵手动指定  │   │  │
│  │ └────────────────────────────────────────────────────────────────────────────┘   │  │
│  │                                                                                    │  │
│  │ ...                                                                                │  │
│  │                                                                                    │  │
│  │ [加载更多]                                                                         │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                          │
│  说明：                                                                                  │
│  • 每行显示一个班次分配                                                                  │
│  • 包含日期时间、哨位、人员信息                                                          │
│  • 支持按日期、哨位、人员排序                                                            │
│  • 支持按哨位、人员、日期分组                                                            │
│  • 标识手动指定和冲突的班次                                                              │
│  • 提供详情和编辑按钮                                                                    │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```


### Personnel View（人员视图）布局

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│  Personnel View - 按人员查看                                                             │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ 选择人员: [张三 ▼]                                                                 │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                          │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ 张三 的排班情况                                                                    │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ 工作量统计                                                                   │   │  │
│  │ │ • 总班次: 18 次                                                              │   │  │
│  │ │ • 日哨: 12 次                                                                │   │  │
│  │ │ • 夜哨: 6 次                                                                 │   │  │
│  │ │ • 工作时长: 36 小时                                                          │   │  │
│  │ └────────────────────────────────────────────────────────────────────────────┘   │  │
│  │                                                                                    │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ 班次列表                                                                     │   │  │
│  │ ├──────────────────┬──────────┬──────────────┬────────────────────────────┤   │  │
│  │ │ 日期时间         │ 哨位     │ 时段         │ 备注                       │   │  │
│  │ ├──────────────────┼──────────┼──────────────┼────────────────────────────┤   │  │
│  │ │ 2025-01-01       │ 哨位1    │ 08:00-10:00  │                            │   │  │
│  │ ├──────────────────┼──────────┼──────────────┼────────────────────────────┤   │  │
│  │ │ 2025-01-03       │ 哨位2    │ 10:00-12:00  │ 🔵手动指定                 │   │  │
│  │ ├──────────────────┼──────────┼──────────────┼────────────────────────────┤   │  │
│  │ │ 2025-01-05       │ 哨位3    │ 22:00-00:00  │ 夜哨                       │   │  │
│  │ ├──────────────────┼──────────┼──────────────┼────────────────────────────┤   │  │
│  │ │ 2025-01-07       │ 哨位1    │ 14:00-16:00  │                            │   │  │
│  │ ├──────────────────┼──────────┼──────────────┼────────────────────────────┤   │  │
│  │ │ ...              │ ...      │ ...          │ ...                        │   │  │
│  │ └──────────────────┴──────────┴──────────────┴────────────────────────────┘   │  │
│  │                                                                                    │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ 日历视图                                                                     │   │  │
│  │ │   周一   周二   周三   周四   周五   周六   周日                             │   │  │
│  │ │ ┌─────┬─────┬─────┬─────┬─────┬─────┬─────┐                           │   │  │
│  │ │ │  1  │  2  │  3  │  4  │  5  │  6  │  7  │                           │   │  │
│  │ │ │  ●  │     │  ●  │     │  ●  │     │     │                           │   │  │
│  │ │ ├─────┼─────┼─────┼─────┼─────┼─────┼─────┤                           │   │  │
│  │ │ │  8  │  9  │ 10  │ 11  │ 12  │ 13  │ 14  │                           │   │  │
│  │ │ │     │  ●  │     │  ●  │     │  ●  │     │                           │   │  │
│  │ │ └─────┴─────┴─────┴─────┴─────┴─────┴─────┘                           │   │  │
│  │ │ ● = 有班次                                                               │   │  │
│  │ └────────────────────────────────────────────────────────────────────────────┘   │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                          │
│  说明：                                                                                  │
│  • 显示选中人员的所有班次                                                                │
│  • 提供工作量统计信息                                                                    │
│  • 班次列表按日期排序                                                                    │
│  • 提供日历视图，直观显示工作日                                                          │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```


### 冲突面板布局

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│  冲突面板（右侧滑出）                                                                     │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ 冲突和警告 (3)                                                    [×关闭]         │  │
│  ├──────────────────────────────────────────────────────────────────────────────────┤  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ ⚠ 技能不匹配                                                                │   │  │
│  │ │ 哨位3 在 2025-01-01 08:00-10:00 分配的人员王五缺少必需技能"高级哨位"       │   │  │
│  │ │ [定位] [忽略] [修复]                                                        │   │  │
│  │ └────────────────────────────────────────────────────────────────────────────┘   │  │
│  │                                                                                    │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ ⚠ 休息时间不足                                                              │   │  │
│  │ │ 人员张三在 2025-01-02 的班次间隔少于8小时                                  │   │  │
│  │ │ [定位] [忽略] [修复]                                                        │   │  │
│  │ └────────────────────────────────────────────────────────────────────────────┘   │  │
│  │                                                                                    │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ ⚠ 工作量不均衡                                                              │   │  │
│  │ │ 人员李四的总班次数(25)超过平均值(18)的30%                                  │   │  │
│  │ │ [定位] [忽略]                                                               │   │  │
│  │ └────────────────────────────────────────────────────────────────────────────┘   │  │
│  │                                                                                    │  │
│  │ [全部忽略] [导出冲突报告]                                                         │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                          │
│  说明：                                                                                  │
│  • 显示所有检测到的冲突和警告                                                            │
│  • 每个冲突显示类型、描述和相关信息                                                      │
│  • 提供定位、忽略、修复操作                                                              │
│  • 点击定位会在主视图中高亮相关单元格                                                    │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```


### 统计信息面板布局

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│  统计信息面板（可折叠）                                                                   │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ ▼ 统计信息                                                                         │  │
│  ├──────────────────────────────────────────────────────────────────────────────────┤  │
│  │ ┌────────────────────┬────────────────────┬────────────────────┐                 │  │
│  │ │ 总分配数           │ 参与人员           │ 参与哨位           │                 │  │
│  │ │ 450 / 500          │ 25 人              │ 10 个              │                 │  │
│  │ │ 90% 覆盖率         │                    │                    │                 │  │
│  │ └────────────────────┴────────────────────┴────────────────────┘                 │  │
│  │                                                                                    │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ 人员工作量统计                                          [按工作量排序▼]     │   │  │
│  │ ├──────────┬──────────┬──────────┬──────────┬──────────────────────────────┤   │  │
│  │ │ 姓名     │ 总班次   │ 日哨     │ 夜哨     │ 工作时长                     │   │  │
│  │ ├──────────┼──────────┼──────────┼──────────┼──────────────────────────────┤   │  │
│  │ │ 张三     │ 18       │ 12       │ 6        │ 36小时  ████████░░ 90%       │   │  │
│  │ ├──────────┼──────────┼──────────┼──────────┼──────────────────────────────┤   │  │
│  │ │ 李四     │ 20       │ 14       │ 6        │ 40小时  ██████████ 100%      │   │  │
│  │ ├──────────┼──────────┼──────────┼──────────┼──────────────────────────────┤   │  │
│  │ │ 王五     │ 16       │ 10       │ 6        │ 32小时  ████████░░ 80%       │   │  │
│  │ ├──────────┼──────────┼──────────┼──────────┼──────────────────────────────┤   │  │
│  │ │ ...      │ ...      │ ...      │ ...      │ ...                          │   │  │
│  │ └──────────┴──────────┴──────────┴──────────┴──────────────────────────────┘   │  │
│  │                                                                                    │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ 哨位覆盖率统计                                          [按覆盖率排序▼]     │   │  │
│  │ ├──────────┬──────────┬──────────┬──────────────────────────────────────────┤   │  │
│  │ │ 哨位     │ 已分配   │ 总时段   │ 覆盖率                                   │   │  │
│  │ ├──────────┼──────────┼──────────┼──────────────────────────────────────────┤   │  │
│  │ │ 哨位1    │ 48       │ 50       │ 96%  █████████░ 96%                      │   │  │
│  │ ├──────────┼──────────┼──────────┼──────────────────────────────────────────┤   │  │
│  │ │ 哨位2    │ 45       │ 50       │ 90%  █████████░ 90%                      │   │  │
│  │ ├──────────┼──────────┼──────────┼──────────────────────────────────────────┤   │  │
│  │ │ 哨位3    │ 42       │ 50       │ 84%  ████████░░ 84%                      │   │  │
│  │ ├──────────┼──────────┼──────────┼──────────────────────────────────────────┤   │  │
│  │ │ ...      │ ...      │ ...      │ ...                                      │   │  │
│  │ └──────────┴──────────┴──────────┴──────────────────────────────────────────┘   │  │
│  │                                                                                    │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ 软约束评分                                                                   │   │  │
│  │ │ • 总分: 85.5 / 100                                                           │   │  │
│  │ │ • 休息时间评分: 90 / 100  █████████░                                         │   │  │
│  │ │ • 时段平衡评分: 82 / 100  ████████░░                                         │   │  │
│  │ │ • 假期平衡评分: 85 / 100  ████████░░                                         │   │  │
│  │ └────────────────────────────────────────────────────────────────────────────┘   │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```


### 单元格详情对话框

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│  班次详情                                                                [×关闭]          │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ 基本信息                                                                           │  │
│  │ • 日期时间: 2025-01-01 08:00-10:00                                                │  │
│  │ • 哨位: 哨位1 (主门岗)                                                            │  │
│  │ • 人员: 张三                                                                       │  │
│  │ • 分配方式: 🔵 手动指定                                                           │  │
│  ├──────────────────────────────────────────────────────────────────────────────────┤  │
│  │ 人员信息                                                                           │  │
│  │ • 技能: 基础哨位, 高级哨位, 夜间值守                                              │  │
│  │ • 当前工作量: 18 班次 (36小时)                                                    │  │
│  │ • 本周工作量: 5 班次 (10小时)                                                     │  │
│  │ • 上一班次: 2025-01-01 06:00-08:00 (哨位2)                                       │  │
│  │ • 下一班次: 2025-01-01 14:00-16:00 (哨位3)                                       │  │
│  ├──────────────────────────────────────────────────────────────────────────────────┤  │
│  │ 约束评估                                                                           │  │
│  │ ✓ 技能匹配                                                                         │  │
│  │ ✓ 休息时间充足 (距上一班次 2小时)                                                 │  │
│  │ ✓ 工作量未超标                                                                     │  │
│  │ ⚠ 连续工作时间较长 (建议休息)                                                     │  │
│  ├──────────────────────────────────────────────────────────────────────────────────┤  │
│  │ 操作                                                                               │  │
│  │ [修改分配] [取消分配] [查看人员详情]                                              │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

### 修改分配对话框

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│  修改班次分配                                                            [×关闭]          │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ 当前分配                                                                           │  │
│  │ • 日期时间: 2025-01-01 08:00-10:00                                                │  │
│  │ • 哨位: 哨位1                                                                      │  │
│  │ • 当前人员: 张三                                                                   │  │
│  ├──────────────────────────────────────────────────────────────────────────────────┤  │
│  │ 选择新人员                                                                         │  │
│  │ ┌────────────────────────────────────────────────────────────────────────────┐   │  │
│  │ │ 搜索: [输入姓名...]                                                          │   │  │
│  │ │                                                                              │   │  │
│  │ │ ○ 李四      ✓技能匹配  ✓可用  工作量: 20班次                                │   │  │
│  │ │ ○ 王五      ✓技能匹配  ✓可用  工作量: 16班次                                │   │  │
│  │ │ ○ 赵六      ⚠技能不足  ✓可用  工作量: 15班次                                │   │  │
│  │ │ ○ 孙七      ✓技能匹配  ×不可用(休假)  工作量: 18班次                        │   │  │
│  │ │ ○ 周八      ✓技能匹配  ⚠工作量高  工作量: 25班次                            │   │  │
│  │ │ ...                                                                          │   │  │
│  │ └────────────────────────────────────────────────────────────────────────────┘   │  │
│  │                                                                                    │  │
│  │ 推荐人员: 李四 (技能匹配, 工作量适中)                                             │  │
│  │                                                                                    │  │
│  │ [确定] [取消]                                                                      │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```



## 架构设计

### 整体架构

系统采用 MVVM 架构模式，主要组件包括：

1. **ScheduleResultPage（视图层）**：WinUI 3 页面，负责UI展示和用户交互
2. **ScheduleResultViewModel（视图模型层）**：管理页面状态、数据和用户命令
3. **SchedulingService（服务层）**：提供排班数据查询和操作服务
4. **ScheduleGridControl（自定义控件）**：排班表格控件，支持虚拟化渲染
5. **PositionScheduleControl（新增控件）**：哨位排班表格控件，按周显示
6. **PersonnelScheduleControl（新增控件）**：人员排班控件，显示个人班次
7. **IScheduleGridExporter（导出服务）**：表格导出服务接口

### 数据流

```
用户操作
  ↓
ScheduleResultPage（视图）
  ↓
ScheduleResultViewModel（视图模型）
  ↓
SchedulingService（服务）
  ↓
Repository（数据访问）
  ↓
Database（数据库）
```

### 视图模式切换流程

```
用户选择视图模式
  ↓
ViewModel.CurrentViewMode 属性变化
  ↓
触发 OnViewModeChanged 方法
  ↓
根据模式加载对应的数据结构
  ↓
更新 UI 绑定属性
  ↓
视图自动切换显示内容
```



## 组件和接口

### 1. ViewMode 枚举（扩展）

```csharp
public enum ViewMode
{
    Grid,        // 网格视图
    List,        // 列表视图
    ByPersonnel, // 人员视图
    ByPosition   // 哨位视图（新增）
}
```

### 2. PositionScheduleData（新增数据模型）

```csharp
/// <summary>
/// 哨位排班数据（按周显示）
/// </summary>
public class PositionScheduleData
{
    /// <summary>
    /// 哨位ID
    /// </summary>
    public int PositionId { get; set; }
    
    /// <summary>
    /// 哨位名称
    /// </summary>
    public string PositionName { get; set; }
    
    /// <summary>
    /// 周次列表
    /// </summary>
    public List<WeekData> Weeks { get; set; }
    
    /// <summary>
    /// 当前选中的周次索引
    /// </summary>
    public int CurrentWeekIndex { get; set; }
}

/// <summary>
/// 周数据
/// </summary>
public class WeekData
{
    /// <summary>
    /// 周次（第几周）
    /// </summary>
    public int WeekNumber { get; set; }
    
    /// <summary>
    /// 周开始日期
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// 周结束日期
    /// </summary>
    public DateTime EndDate { get; set; }
    
    /// <summary>
    /// 单元格数据（键为 "periodIndex_dayOfWeek"）
    /// periodIndex: 0-11（12个时段）
    /// dayOfWeek: 0-6（周一到周日）
    /// </summary>
    public Dictionary<string, PositionScheduleCell> Cells { get; set; }
}

/// <summary>
/// 哨位排班单元格
/// </summary>
public class PositionScheduleCell
{
    /// <summary>
    /// 时段索引（0-11）
    /// </summary>
    public int PeriodIndex { get; set; }
    
    /// <summary>
    /// 星期（0=周一, 6=周日）
    /// </summary>
    public int DayOfWeek { get; set; }
    
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// 人员ID
    /// </summary>
    public int? PersonnelId { get; set; }
    
    /// <summary>
    /// 人员姓名
    /// </summary>
    public string? PersonnelName { get; set; }
    
    /// <summary>
    /// 是否已分配
    /// </summary>
    public bool IsAssigned { get; set; }
    
    /// <summary>
    /// 是否手动指定
    /// </summary>
    public bool IsManualAssignment { get; set; }
    
    /// <summary>
    /// 是否有冲突
    /// </summary>
    public bool HasConflict { get; set; }
    
    /// <summary>
    /// 冲突消息
    /// </summary>
    public string? ConflictMessage { get; set; }
}
```



### 3. PersonnelScheduleData（新增数据模型）

```csharp
/// <summary>
/// 人员排班数据
/// </summary>
public class PersonnelScheduleData
{
    /// <summary>
    /// 人员ID
    /// </summary>
    public int PersonnelId { get; set; }
    
    /// <summary>
    /// 人员姓名
    /// </summary>
    public string PersonnelName { get; set; }
    
    /// <summary>
    /// 工作量统计
    /// </summary>
    public PersonnelWorkload Workload { get; set; }
    
    /// <summary>
    /// 班次列表
    /// </summary>
    public List<PersonnelShift> Shifts { get; set; }
    
    /// <summary>
    /// 日历数据（用于日历视图）
    /// </summary>
    public Dictionary<DateTime, List<PersonnelShift>> CalendarData { get; set; }
}

/// <summary>
/// 人员班次
/// </summary>
public class PersonnelShift
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// 时段索引
    /// </summary>
    public int PeriodIndex { get; set; }
    
    /// <summary>
    /// 时段描述（如 "08:00-10:00"）
    /// </summary>
    public string TimeSlot { get; set; }
    
    /// <summary>
    /// 哨位ID
    /// </summary>
    public int PositionId { get; set; }
    
    /// <summary>
    /// 哨位名称
    /// </summary>
    public string PositionName { get; set; }
    
    /// <summary>
    /// 是否手动指定
    /// </summary>
    public bool IsManualAssignment { get; set; }
    
    /// <summary>
    /// 是否夜哨
    /// </summary>
    public bool IsNightShift { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
}
```

### 4. ShiftListItem（列表视图数据模型）

```csharp
/// <summary>
/// 班次列表项（用于 List View）
/// </summary>
public class ShiftListItem
{
    /// <summary>
    /// 班次ID
    /// </summary>
    public int ShiftId { get; set; }
    
    /// <summary>
    /// 日期时间描述
    /// </summary>
    public string DateTimeDescription { get; set; }
    
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// 时段索引
    /// </summary>
    public int PeriodIndex { get; set; }
    
    /// <summary>
    /// 时段描述
    /// </summary>
    public string TimeSlot { get; set; }
    
    /// <summary>
    /// 哨位ID
    /// </summary>
    public int PositionId { get; set; }
    
    /// <summary>
    /// 哨位名称
    /// </summary>
    public string PositionName { get; set; }
    
    /// <summary>
    /// 人员ID
    /// </summary>
    public int PersonnelId { get; set; }
    
    /// <summary>
    /// 人员姓名
    /// </summary>
    public string PersonnelName { get; set; }
    
    /// <summary>
    /// 是否手动指定
    /// </summary>
    public bool IsManualAssignment { get; set; }
    
    /// <summary>
    /// 是否有冲突
    /// </summary>
    public bool HasConflict { get; set; }
    
    /// <summary>
    /// 冲突消息
    /// </summary>
    public string? ConflictMessage { get; set; }
}
```



### 5. ScheduleResultViewModel（扩展）

```csharp
public partial class ScheduleResultViewModel : ObservableObject
{
    #region 依赖注入
    
    private readonly ISchedulingService _schedulingService;
    private readonly NavigationService _navigationService;
    private readonly IScheduleGridExporter _gridExporter;
    private readonly DialogService _dialogService;
    private readonly IHistoryManagement _historyManagement;
    
    #endregion
    
    #region 基础属性
    
    [ObservableProperty]
    private ScheduleDto? _schedule;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private ViewMode _currentViewMode = ViewMode.Grid;
    
    [ObservableProperty]
    private bool _isConflictPaneOpen;
    
    [ObservableProperty]
    private bool _hasUnsavedChanges;
    
    #endregion
    
    #region Grid View 数据
    
    [ObservableProperty]
    private ScheduleGridData? _gridData;
    
    #endregion
    
    #region Position View 数据（新增）
    
    [ObservableProperty]
    private ObservableCollection<PositionScheduleData> _positionSchedules = new();
    
    [ObservableProperty]
    private PositionScheduleData? _selectedPositionSchedule;
    
    [ObservableProperty]
    private int _currentWeekIndex;
    
    #endregion
    
    #region Personnel View 数据（新增）
    
    [ObservableProperty]
    private ObservableCollection<PersonnelScheduleData> _personnelSchedules = new();
    
    [ObservableProperty]
    private PersonnelScheduleData? _selectedPersonnelSchedule;
    
    #endregion
    
    #region List View 数据（新增）
    
    [ObservableProperty]
    private ObservableCollection<ShiftListItem> _shiftList = new();
    
    [ObservableProperty]
    private string _listSortBy = "Date";
    
    [ObservableProperty]
    private string _listGroupBy = "None";
    
    #endregion
    
    #region 统计数据
    
    [ObservableProperty]
    private ObservableCollection<PersonnelWorkload> _personnelWorkloads = new();
    
    [ObservableProperty]
    private ObservableCollection<PositionCoverage> _positionCoverages = new();
    
    [ObservableProperty]
    private SchedulingStatistics? _statistics;
    
    #endregion
    
    #region 冲突数据
    
    [ObservableProperty]
    private ObservableCollection<ConflictInfo> _conflicts = new();
    
    #endregion
    
    #region 筛选条件（新增）
    
    [ObservableProperty]
    private DateTime _filterStartDate;
    
    [ObservableProperty]
    private DateTime _filterEndDate;
    
    [ObservableProperty]
    private ObservableCollection<int> _selectedPositionIds = new();
    
    [ObservableProperty]
    private string _personnelSearchText = string.Empty;
    
    #endregion
    
    #region 命令
    
    public IAsyncRelayCommand<int> LoadScheduleCommand { get; }
    public IAsyncRelayCommand ExportExcelCommand { get; }
    public IAsyncRelayCommand<string> ExportCommand { get; }
    public IRelayCommand BackCommand { get; }
    public IAsyncRelayCommand RescheduleCommand { get; }
    public IAsyncRelayCommand ConfirmCommand { get; }
    public IRelayCommand ToggleConflictPaneCommand { get; }
    public IRelayCommand<ViewMode> SwitchViewModeCommand { get; }
    
    // 新增命令
    public IAsyncRelayCommand ApplyFiltersCommand { get; }
    public IAsyncRelayCommand ResetFiltersCommand { get; }
    public IAsyncRelayCommand<PositionScheduleData> SelectPositionCommand { get; }
    public IAsyncRelayCommand<PersonnelScheduleData> SelectPersonnelCommand { get; }
    public IRelayCommand<int> ChangeWeekCommand { get; }
    public IAsyncRelayCommand<ShiftListItem> ViewShiftDetailsCommand { get; }
    public IAsyncRelayCommand<ShiftListItem> EditShiftCommand { get; }
    public IAsyncRelayCommand SaveChangesCommand { get; }
    public IRelayCommand DiscardChangesCommand { get; }
    public IAsyncRelayCommand<ConflictInfo> LocateConflictCommand { get; }
    public IAsyncRelayCommand<ConflictInfo> IgnoreConflictCommand { get; }
    public IAsyncRelayCommand<ConflictInfo> FixConflictCommand { get; }
    public IAsyncRelayCommand ToggleFullScreenCommand { get; }
    public IAsyncRelayCommand CompareSchedulesCommand { get; }
    
    #endregion
    
    #region 方法
    
    /// <summary>
    /// 加载排班数据
    /// </summary>
    private async Task LoadScheduleAsync(int scheduleId);
    
    /// <summary>
    /// 构建 Position View 数据
    /// </summary>
    private async Task BuildPositionScheduleDataAsync();
    
    /// <summary>
    /// 构建 Personnel View 数据
    /// </summary>
    private async Task BuildPersonnelScheduleDataAsync();
    
    /// <summary>
    /// 构建 List View 数据
    /// </summary>
    private async Task BuildShiftListDataAsync();
    
    /// <summary>
    /// 应用筛选条件
    /// </summary>
    private async Task ApplyFiltersAsync();
    
    /// <summary>
    /// 视图模式变化处理
    /// </summary>
    private async Task OnViewModeChangedAsync(ViewMode newMode);
    
    /// <summary>
    /// 编辑班次分配
    /// </summary>
    private async Task EditShiftAssignmentAsync(ShiftListItem shift);
    
    /// <summary>
    /// 检测冲突
    /// </summary>
    private async Task DetectConflictsAsync();
    
    #endregion
}
```



### 6. PositionScheduleControl（新增自定义控件）

```csharp
/// <summary>
/// 哨位排班表格控件（按周显示）
/// </summary>
public class PositionScheduleControl : Control
{
    #region 依赖属性
    
    public static readonly DependencyProperty ScheduleDataProperty =
        DependencyProperty.Register(
            nameof(ScheduleData),
            typeof(PositionScheduleData),
            typeof(PositionScheduleControl),
            new PropertyMetadata(null, OnScheduleDataChanged));
    
    public PositionScheduleData ScheduleData
    {
        get => (PositionScheduleData)GetValue(ScheduleDataProperty);
        set => SetValue(ScheduleDataProperty, value);
    }
    
    #endregion
    
    #region 事件
    
    public event EventHandler<CellClickedEventArgs> CellClicked;
    public event EventHandler<WeekChangedEventArgs> WeekChanged;
    
    #endregion
    
    #region 方法
    
    private void OnScheduleDataChanged(PositionScheduleData newData)
    {
        BuildWeeklyGrid(newData);
    }
    
    private void BuildWeeklyGrid(PositionScheduleData data)
    {
        // 构建周视图表格
        // 行：12个时段
        // 列：7天（周一到周日）
    }
    
    #endregion
}
```

### 7. PersonnelScheduleControl（新增自定义控件）

```csharp
/// <summary>
/// 人员排班控件
/// </summary>
public class PersonnelScheduleControl : Control
{
    #region 依赖属性
    
    public static readonly DependencyProperty ScheduleDataProperty =
        DependencyProperty.Register(
            nameof(ScheduleData),
            typeof(PersonnelScheduleData),
            typeof(PersonnelScheduleControl),
            new PropertyMetadata(null, OnScheduleDataChanged));
    
    public PersonnelScheduleData ScheduleData
    {
        get => (PersonnelScheduleData)GetValue(ScheduleDataProperty);
        set => SetValue(ScheduleDataProperty, value);
    }
    
    #endregion
    
    #region 方法
    
    private void OnScheduleDataChanged(PersonnelScheduleData newData)
    {
        BuildPersonnelView(newData);
    }
    
    private void BuildPersonnelView(PersonnelScheduleData data)
    {
        // 构建人员视图
        // 包含工作量统计、班次列表、日历视图
    }
    
    #endregion
}
```



## 数据转换逻辑

### 1. 构建 Position View 数据

```csharp
private async Task<List<PositionScheduleData>> BuildPositionScheduleDataAsync(ScheduleDto schedule)
{
    var positionSchedules = new List<PositionScheduleData>();
    
    // 遍历每个哨位
    foreach (var positionId in schedule.PositionIds)
    {
        var positionSchedule = new PositionScheduleData
        {
            PositionId = positionId,
            PositionName = await GetPositionNameAsync(positionId),
            Weeks = new List<WeekData>(),
            CurrentWeekIndex = 0
        };
        
        // 计算周数
        var totalDays = (schedule.EndDate - schedule.StartDate).Days + 1;
        var totalWeeks = (int)Math.Ceiling(totalDays / 7.0);
        
        // 构建每周的数据
        for (int weekIndex = 0; weekIndex < totalWeeks; weekIndex++)
        {
            var weekStartDate = schedule.StartDate.AddDays(weekIndex * 7);
            var weekEndDate = weekStartDate.AddDays(6);
            if (weekEndDate > schedule.EndDate)
                weekEndDate = schedule.EndDate;
            
            var weekData = new WeekData
            {
                WeekNumber = weekIndex + 1,
                StartDate = weekStartDate,
                EndDate = weekEndDate,
                Cells = new Dictionary<string, PositionScheduleCell>()
            };
            
            // 填充单元格数据
            for (int periodIndex = 0; periodIndex < 12; periodIndex++)
            {
                for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
                {
                    var date = weekStartDate.AddDays(dayOfWeek);
                    if (date > schedule.EndDate)
                        continue;
                    
                    var cellKey = $"{periodIndex}_{dayOfWeek}";
                    
                    // 查找该时段的分配
                    var shift = schedule.Shifts.FirstOrDefault(s =>
                        s.PositionId == positionId &&
                        s.Date == date &&
                        s.PeriodIndex == periodIndex);
                    
                    weekData.Cells[cellKey] = new PositionScheduleCell
                    {
                        PeriodIndex = periodIndex,
                        DayOfWeek = dayOfWeek,
                        Date = date,
                        PersonnelId = shift?.PersonnelId,
                        PersonnelName = shift != null ? await GetPersonnelNameAsync(shift.PersonnelId) : null,
                        IsAssigned = shift != null,
                        IsManualAssignment = shift?.IsManualAssignment ?? false,
                        HasConflict = false // 需要检测冲突
                    };
                }
            }
            
            positionSchedule.Weeks.Add(weekData);
        }
        
        positionSchedules.Add(positionSchedule);
    }
    
    return positionSchedules;
}
```

### 2. 构建 Personnel View 数据

```csharp
private async Task<List<PersonnelScheduleData>> BuildPersonnelScheduleDataAsync(ScheduleDto schedule)
{
    var personnelSchedules = new List<PersonnelScheduleData>();
    
    // 遍历每个人员
    foreach (var personnelId in schedule.PersonnelIds)
    {
        var personnelSchedule = new PersonnelScheduleData
        {
            PersonnelId = personnelId,
            PersonnelName = await GetPersonnelNameAsync(personnelId),
            Shifts = new List<PersonnelShift>(),
            CalendarData = new Dictionary<DateTime, List<PersonnelShift>>()
        };
        
        // 获取该人员的所有班次
        var personnelShifts = schedule.Shifts
            .Where(s => s.PersonnelId == personnelId)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.PeriodIndex);
        
        foreach (var shift in personnelShifts)
        {
            var personnelShift = new PersonnelShift
            {
                Date = shift.Date,
                PeriodIndex = shift.PeriodIndex,
                TimeSlot = GetTimeSlotDescription(shift.PeriodIndex),
                PositionId = shift.PositionId,
                PositionName = await GetPositionNameAsync(shift.PositionId),
                IsManualAssignment = shift.IsManualAssignment,
                IsNightShift = IsNightShift(shift.PeriodIndex),
                Remarks = GetShiftRemarks(shift)
            };
            
            personnelSchedule.Shifts.Add(personnelShift);
            
            // 添加到日历数据
            if (!personnelSchedule.CalendarData.ContainsKey(shift.Date))
            {
                personnelSchedule.CalendarData[shift.Date] = new List<PersonnelShift>();
            }
            personnelSchedule.CalendarData[shift.Date].Add(personnelShift);
        }
        
        // 计算工作量
        personnelSchedule.Workload = CalculatePersonnelWorkload(personnelSchedule.Shifts);
        
        personnelSchedules.Add(personnelSchedule);
    }
    
    return personnelSchedules;
}
```

### 3. 构建 List View 数据

```csharp
private async Task<List<ShiftListItem>> BuildShiftListDataAsync(ScheduleDto schedule)
{
    var shiftList = new List<ShiftListItem>();
    
    foreach (var shift in schedule.Shifts)
    {
        var listItem = new ShiftListItem
        {
            ShiftId = shift.Id,
            Date = shift.Date,
            PeriodIndex = shift.PeriodIndex,
            TimeSlot = GetTimeSlotDescription(shift.PeriodIndex),
            DateTimeDescription = $"{shift.Date:yyyy-MM-dd} {GetTimeSlotDescription(shift.PeriodIndex)}",
            PositionId = shift.PositionId,
            PositionName = await GetPositionNameAsync(shift.PositionId),
            PersonnelId = shift.PersonnelId,
            PersonnelName = await GetPersonnelNameAsync(shift.PersonnelId),
            IsManualAssignment = shift.IsManualAssignment,
            HasConflict = false, // 需要检测冲突
            ConflictMessage = null
        };
        
        shiftList.Add(listItem);
    }
    
    return shiftList;
}
```



## 交互设计

### 1. 视图模式切换

- 用户点击视图模式单选按钮
- ViewModel 的 CurrentViewMode 属性变化
- 触发 OnViewModeChanged 方法
- 根据新模式加载对应的数据结构
- UI 通过数据绑定自动切换显示内容

### 2. 筛选功能

- 用户设置筛选条件（日期范围、哨位、人员）
- 点击"应用"按钮
- ViewModel 执行 ApplyFiltersCommand
- 根据筛选条件重新构建数据
- 更新 UI 显示

### 3. 单元格交互

**鼠标悬停：**
- 显示 Tooltip，包含人员详细信息
- 技能、工作量、上下班次等

**单击：**
- 高亮选中的单元格
- 在冲突面板中显示相关冲突（如有）

**双击：**
- 打开修改分配对话框
- 显示可用人员列表
- 标注技能匹配情况和工作量

### 4. 冲突处理

**定位冲突：**
- 用户点击冲突项的"定位"按钮
- 系统切换到 Grid View（如果当前不是）
- 滚动到相关单元格并高亮显示

**忽略冲突：**
- 用户点击"忽略"按钮
- 系统将冲突标记为已忽略
- 从冲突列表中移除（但保留记录）

**修复冲突：**
- 用户点击"修复"按钮
- 系统打开修改分配对话框
- 推荐合适的人员替换

### 5. 编辑功能

**修改分配：**
1. 用户双击单元格或点击"编辑"按钮
2. 打开修改分配对话框
3. 显示当前分配和可用人员列表
4. 用户选择新人员并确认
5. 系统更新分配并重新检测冲突
6. 标记为未保存状态

**保存更改：**
1. 用户点击"保存"按钮
2. 系统验证所有更改
3. 将更改保存到数据库
4. 清除未保存标记

**撤销更改：**
1. 用户点击"撤销"按钮
2. 系统显示确认对话框
3. 用户确认后恢复原始数据
4. 清除未保存标记



## 错误处理

### 1. 数据加载错误

```csharp
try
{
    await LoadScheduleAsync(scheduleId);
}
catch (ArgumentException ex)
{
    await _dialogService.ShowErrorAsync("参数错误", $"无效的排班ID：{ex.Message}");
    _navigationService.NavigateBack();
}
catch (InvalidOperationException ex)
{
    await _dialogService.ShowErrorAsync("加载失败", $"排班数据不存在或已被删除：{ex.Message}");
    _navigationService.NavigateBack();
}
catch (Exception ex)
{
    await _dialogService.ShowErrorAsync("系统错误", $"加载排班数据时发生错误：{ex.Message}");
}
```

### 2. 视图切换错误

```csharp
try
{
    await OnViewModeChangedAsync(newMode);
}
catch (Exception ex)
{
    await _dialogService.ShowErrorAsync("视图切换失败", $"切换到 {newMode} 视图时发生错误：{ex.Message}");
    // 恢复到之前的视图模式
    CurrentViewMode = previousMode;
}
```

### 3. 编辑操作错误

```csharp
try
{
    await UpdateShiftAssignmentAsync(shift, newPersonnelId);
}
catch (InvalidOperationException ex)
{
    await _dialogService.ShowErrorAsync("修改失败", $"无法修改班次分配：{ex.Message}");
}
catch (Exception ex)
{
    await _dialogService.ShowErrorAsync("系统错误", $"修改班次时发生错误：{ex.Message}");
}
```

### 4. 导出错误

```csharp
try
{
    await ExportScheduleAsync(format);
}
catch (NotImplementedException)
{
    await _dialogService.ShowWarningAsync("功能开发中", $"{format} 格式导出功能正在开发中");
}
catch (IOException ex)
{
    await _dialogService.ShowErrorAsync("导出失败", $"文件写入失败：{ex.Message}");
}
catch (Exception ex)
{
    await _dialogService.ShowErrorAsync("导出失败", $"导出排班数据时发生错误：{ex.Message}");
}
```

## 性能优化

### 1. 虚拟化渲染

- Grid View 使用虚拟化面板，仅渲染可见区域
- List View 使用 ListView 的内置虚拟化
- Position View 和 Personnel View 也采用虚拟化技术

### 2. 数据缓存

```csharp
private Dictionary<int, string> _personnelNameCache = new();
private Dictionary<int, string> _positionNameCache = new();

private async Task<string> GetPersonnelNameAsync(int personnelId)
{
    if (_personnelNameCache.TryGetValue(personnelId, out var name))
        return name;
    
    name = await _personnelService.GetPersonnelNameAsync(personnelId);
    _personnelNameCache[personnelId] = name;
    return name;
}
```

### 3. 延迟加载

- 统计信息面板默认折叠，展开时才加载数据
- Personnel View 和 Position View 在切换时才构建数据
- 冲突检测在后台异步执行

### 4. 批量操作

- 多个单元格修改时，批量更新数据库
- 批量检测冲突，避免重复计算

## 测试策略

### 1. 单元测试

- ViewModel 的数据转换逻辑
- 筛选和排序逻辑
- 冲突检测逻辑
- 命令执行逻辑

### 2. 集成测试

- 视图模式切换流程
- 编辑和保存流程
- 导出功能
- 冲突处理流程

### 3. UI 测试

- 各视图模式的显示正确性
- 交互响应性
- 大数据集性能测试
- 响应式布局适配

## 可访问性

### 1. 屏幕阅读器支持

- 所有控件设置 AutomationProperties.Name
- 表格使用语义化标记
- 状态变化通知屏幕阅读器

### 2. 键盘导航

- 所有功能支持键盘操作
- Tab 键顺序合理
- 快捷键支持：
  - Ctrl+F：打开筛选
  - Ctrl+E：导出
  - Ctrl+S：保存更改
  - Esc：关闭对话框

### 3. 高对比度主题

- 支持系统高对比度主题
- 冲突和警告使用明显的视觉标识
- 手动指定的单元格有明显标记

## 国际化

### 1. 多语言支持

- 所有文本使用资源文件
- 日期和时间格式本地化
- 数字格式本地化

### 2. 资源文件结构

```
Resources/
  zh-CN/
    ScheduleResult.resw
  en-US/
    ScheduleResult.resw
```

## 扩展性

### 1. 自定义视图模式

- 预留接口支持第三方扩展视图模式
- 插件架构设计

### 2. 自定义导出格式

- IScheduleGridExporter 接口支持扩展
- 可注册自定义导出器

### 3. 自定义冲突检测规则

- 预留接口支持自定义冲突检测逻辑
- 可配置冲突严重级别

