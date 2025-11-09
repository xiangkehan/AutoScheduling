# 实现计划 - 测试数据生成器

- [x] 1. 创建核心数据生成类





  - 创建TestDataConfiguration配置类，支持预设和自定义配置
  - 创建SampleDataProvider类，提供中文示例数据（姓名、技能、哨位等）
  - 实现TestDataGenerator主生成器类的基础结构
  - _需求: 1.1, 1.2, 1.3, 1.4, 1.5_


- [x] 2. 实现基础实体数据生成



  - [x] 2.1 实现技能数据生成方法


    - 生成唯一的技能名称和描述
    - 设置合理的IsActive状态和时间戳
    - _需求: 2.1, 2.2, 2.3, 2.4, 2.5_
  

  - [-] 2.2 实现人员数据生成方法

    - 生成真实的中文姓名
    - 随机分配1-3个技能并确保引用正确
    - 初始化12个时段的班次间隔数组
    - _需求: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_
  


  - [ ] 2.3 实现哨位数据生成方法
    - 生成哨位名称、地点和描述
    - 分配所需技能和符合条件的可用人员
    - 确保引用关系正确
    - _需求: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_



- [x] 3. 实现配置和约束数据生成



  - [ ] 3.1 实现节假日配置生成方法
    - 生成标准周末配置和单休配置


    - 添加法定节假日和自定义休息日
    - _需求: 5.1, 5.2, 5.3, 5.4, 5.5_
  
  - [x] 3.2 实现排班模板生成方法

    - 生成regular、holiday、special三种类型模板

    - 分配人员和哨位，确保引用正确
    - 设置合理的排班天数和使用次数


    - _需求: 6.1, 6.2, 6.3, 6.4, 6.5_
  
  - [x] 3.3 实现定岗规则生成方法



    - 为人员指定允许的哨位和时段


    - 设置合理的日期范围
    - _需求: 7.1, 7.2, 7.3, 7.4, 7.5_
  


  - [ ] 3.4 实现手动指定生成方法
    - 生成哨位、人员、日期、时段的组合
    - 避免重复组合
    - 添加有意义的备注
    - _需求: 8.1, 8.2, 8.3, 8.4, 8.5_


- [ ] 4. 实现数据导出功能

  - [ ] 4.1 实现元数据生成
    - 创建ExportMetadata对象
    - 计算数据统计信息
    - _需求: 9.3_
  
  - [ ] 4.2 实现JSON序列化导出
    - 使用与DataImportExportService相同的序列化选项
    - 支持导出到StorageFile（WinUI3方式）
    - 支持导出到文件路径（传统方式）
    - _需求: 9.1, 9.2, 9.4, 9.5_

- [ ] 5. 创建文件位置管理器

  - [ ] 5.1 实现FileLocationManager类
    - 使用ApplicationData.LocalFolder作为默认存储
    - 实现GetTestDataFolderAsync方法创建TestData文件夹
    - 实现GenerateNewFileName方法生成时间戳文件名
    - 实现CreateNewTestDataFileAsync方法创建文件

    - _需求: 9.5_
  
  - [ ] 5.2 实现最近文件管理
    - 使用FutureAccessList保存文件访问令牌
    - 使用LocalSettings持久化最近文件列表
    - 实现AddToRecentFilesAsync方法
    - 实现GetRecentTestDataFilesAsync方法
    - 限制最多20个最近文件
    - _需求: 9.5_
  
  - [ ] 5.3 实现文件清理功能
    - 实现CleanOldFilesAsync方法
    - 支持按天数清理旧文件
    - _需求: 9.5_

- [ ] 6. 创建UI页面和ViewModel

  - [ ] 6.1 创建TestDataGeneratorViewModel
    - 实现配置属性（数据规模、自定义配置）
    - 实现生成选项属性
    - 实现最近文件列表
    - 实现状态和进度属性
    - _需求: 10.1, 10.2_
  
  - [ ] 6.2 实现ViewModel命令
    - 实现GenerateCommand生成测试数据
    - 实现BrowseCommand使用FileSavePicker选择位置
    - 实现ImportFileCommand导入测试数据
    - 实现OpenFileLocationCommand打开文件位置
    - 实现CleanOldFilesCommand清理旧文件
    - 实现RefreshRecentFilesCommand刷新列表
    - _需求: 10.3_

  
  - [ ] 6.3 创建TestDataGeneratorPage.xaml
    - 创建数据规模选择UI（RadioButtons）
    - 创建自定义配置面板（NumericUpDown控件）
    - 创建生成选项UI（CheckBox）

    - 创建输出文件配置UI
    - 创建操作按钮
    - 创建进度显示UI
    - 创建最近文件列表UI
    - _需求: 10.1, 10.2, 10.3_
  
  - [ ] 6.4 实现TestDataGeneratorPage.xaml.cs
    - 初始化ViewModel
    - 处理页面生命周期
    - _需求: 10.1_


- [ ] 7. 集成到设置页面

  - 在SettingsPage中添加"测试数据生成器"导航项
  - 配置页面路由
  - _需求: 10.1_

- [ ] 8. 实现数据验证

  - [ ] 8.1 实现配置验证
    - 验证配置参数的合理性
    - 提供友好的错误消息
    - _需求: 1.3_
  
  - [ ] 8.2 实现生成数据验证
    - 验证引用完整性
    - 验证数据符合DTO规则
    - _需求: 1.3, 1.4_

- [ ] 9. 配置WinUI3权限

  - 在Package.appxmanifest中添加documentsLibrary权限（可选）
  - 配置文件类型关联（可选）
  - _需求: 9.5_

- [ ] 10. 创建使用示例和文档
  - 创建GenerateTestDataExample.cs示例代码
  - 添加代码注释和使用说明
  - _需求: 10.1, 10.2, 10.3, 10.4, 10.5_

- [ ]* 11. 测试和验证
  - [ ]* 11.1 单元测试
    - 测试每个生成方法的输出
    - 验证数据数量符合配置
    - 验证引用关系正确性
    - 验证数据符合DTO验证规则
  
  - [ ]* 11.2 集成测试
    - 生成完整数据集
    - 导出到JSON文件
    - 使用DataImportExportService导入
    - 验证导入成功且数据完整
  
  - [ ]* 11.3 UI测试
    - 测试页面交互
    - 测试文件选择器
    - 测试进度显示
    - 测试最近文件列表
