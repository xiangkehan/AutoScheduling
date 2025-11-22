using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.ImportExport;
using AutoScheduling3.TestData.Generators;
using AutoScheduling3.TestData.Helpers;
using AutoScheduling3.TestData.Validation;
using AutoScheduling3.TestData.Export;
using System.Text.Json;
using Windows.Storage;

namespace AutoScheduling3.TestData;

/// <summary>
/// 测试数据生成器主类（协调器）
/// 作为门面模式的实现，协调各个生成器、验证器和导出器完成测试数据的生成、验证和导出。
/// 该类不直接实现数据生成逻辑，而是将职责委托给专门的组件：
/// - 生成器（Generators）：负责生成各类实体数据
/// - 验证器（Validator）：负责验证数据的完整性和正确性
/// - 导出器（Exporter）：负责将数据导出到文件或字符串
/// - 辅助工具（Helpers）：提供名称生成、元数据构建等辅助功能
/// </summary>
public class TestDataGenerator
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly Random _random;
    
    // 生成器
    private readonly SkillGenerator _skillGenerator;
    private readonly PersonnelGenerator _personnelGenerator;
    private readonly PositionGenerator _positionGenerator;
    private readonly SkillAssigner _skillAssigner;
    private readonly HolidayConfigGenerator _holidayConfigGenerator;
    private readonly TemplateGenerator _templateGenerator;
    private readonly FixedAssignmentGenerator _fixedAssignmentGenerator;
    private readonly ManualAssignmentGenerator _manualAssignmentGenerator;
    
    // 辅助工具
    private readonly UniqueNameGenerator _nameGenerator;
    private readonly TestDataValidator _validator;
    private readonly ExportMetadataBuilder _metadataBuilder;
    private readonly TestDataExporter _exporter;

    /// <summary>
    /// 使用默认配置创建测试数据生成器
    /// </summary>
    public TestDataGenerator() : this(TestDataConfiguration.CreateDefault())
    {
    }

    /// <summary>
    /// 使用指定配置创建测试数据生成器
    /// </summary>
    public TestDataGenerator(TestDataConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _config.Validate();

        _random = new Random(_config.RandomSeed);
        _sampleData = new SampleDataProvider(_random);
        
        // 初始化辅助工具
        _nameGenerator = new UniqueNameGenerator(_random);
        _validator = new TestDataValidator();
        _metadataBuilder = new ExportMetadataBuilder();
        _exporter = new TestDataExporter();
        
        // 初始化生成器
        _skillGenerator = new SkillGenerator(_config, _sampleData, _nameGenerator, _random);
        _personnelGenerator = new PersonnelGenerator(_config, _sampleData, _nameGenerator, _random);
        _positionGenerator = new PositionGenerator(_config, _sampleData, _nameGenerator, _random);
        _skillAssigner = new SkillAssigner(_config, _random);
        _holidayConfigGenerator = new HolidayConfigGenerator(_config, _random);
        _templateGenerator = new TemplateGenerator(_config, _sampleData, _random);
        _fixedAssignmentGenerator = new FixedAssignmentGenerator(_config, _random);
        _manualAssignmentGenerator = new ManualAssignmentGenerator(_config, _sampleData, _random);
    }

    /// <summary>
    /// 生成完整的测试数据集
    /// 按照依赖顺序调用各个生成器，创建元数据，验证数据完整性
    /// </summary>
    /// <returns>包含所有生成数据和元数据的ExportData对象</returns>
    public ExportData GenerateTestData()
    {
        System.Diagnostics.Debug.WriteLine("=== 开始生成测试数据 ===");
        System.Diagnostics.Debug.WriteLine($"配置：技能={_config.SkillCount}, 人员={_config.PersonnelCount}, " +
            $"哨位={_config.PositionCount}, 每个哨位最小人员数={_config.MinPersonnelPerPosition}, " +
            $"人员可用率={_config.PersonnelAvailabilityRate:P0}, 人员退役率={_config.PersonnelRetirementRate:P0}");
        System.Diagnostics.Debug.WriteLine($"其他配置：节假日配置={_config.HolidayConfigCount}, " +
            $"模板={_config.TemplateCount}, 定岗规则={_config.FixedAssignmentCount}, " +
            $"手动指定={_config.ManualAssignmentCount}");
        
        // 按依赖顺序调用各个生成器
        // 1. 技能（无依赖）
        var skills = _skillGenerator.Generate();
        System.Diagnostics.Debug.WriteLine($"✓ 生成技能数据：{skills.Count} 条");
        
        // 2. 人员（无技能）
        var personnel = _personnelGenerator.Generate();
        System.Diagnostics.Debug.WriteLine($"✓ 生成人员数据：{personnel.Count} 条（暂无技能）");
        
        // 3. 哨位（含技能需求，但无可用人员）
        var positions = _positionGenerator.Generate(skills);
        System.Diagnostics.Debug.WriteLine($"✓ 生成哨位数据：{positions.Count} 条（含技能需求）");
        
        // 4. 根据哨位需求为人员分配技能
        System.Diagnostics.Debug.WriteLine("开始智能技能分配...");
        personnel = _skillAssigner.AssignSkills(personnel, positions, skills);
        System.Diagnostics.Debug.WriteLine($"✓ 技能分配完成");
        
        // 5. 更新哨位的可用人员列表
        UpdatePositionAvailablePersonnel(positions, personnel);
        System.Diagnostics.Debug.WriteLine($"✓ 更新哨位可用人员列表完成");
        
        // 6. 节假日配置（无依赖）
        var holidayConfigs = _holidayConfigGenerator.Generate();
        System.Diagnostics.Debug.WriteLine($"✓ 生成节假日配置：{holidayConfigs.Count} 条");
        
        // 7. 排班模板（依赖人员、哨位和节假日配置）
        var templates = _templateGenerator.Generate(personnel, positions, holidayConfigs);
        System.Diagnostics.Debug.WriteLine($"✓ 生成排班模板：{templates.Count} 条");
        
        // 8. 定岗规则（依赖人员和哨位）
        var fixedAssignments = _fixedAssignmentGenerator.Generate(personnel, positions);
        System.Diagnostics.Debug.WriteLine($"✓ 生成定岗规则：{fixedAssignments.Count} 条");
        
        // 9. 手动指定（依赖人员和哨位）
        var manualAssignments = _manualAssignmentGenerator.Generate(personnel, positions);
        System.Diagnostics.Debug.WriteLine($"✓ 生成手动指定：{manualAssignments.Count} 条");

        // 创建导出数据对象
        var exportData = new ExportData
        {
            Skills = skills,
            Personnel = personnel,
            Positions = positions,
            HolidayConfigs = holidayConfigs,
            Templates = templates,
            FixedAssignments = fixedAssignments,
            ManualAssignments = manualAssignments
        };

        // 使用 ExportMetadataBuilder 创建元数据
        exportData.Metadata = _metadataBuilder.Build(exportData);

        // 使用 TestDataValidator 验证数据
        System.Diagnostics.Debug.WriteLine("开始验证生成的数据...");
        _validator.Validate(exportData);
        System.Diagnostics.Debug.WriteLine("✓ 数据验证通过");
        
        System.Diagnostics.Debug.WriteLine("=== 测试数据生成完成 ===");

        return exportData;
    }

    /// <summary>
    /// 更新哨位的可用人员列表
    /// 根据人员的技能和状态，计算每个哨位的可用人员
    /// </summary>
    /// <param name="positions">哨位列表</param>
    /// <param name="personnel">人员列表（含技能）</param>
    private void UpdatePositionAvailablePersonnel(
        List<PositionDto> positions,
        List<PersonnelDto> personnel)
    {
        foreach (var position in positions)
        {
            // 筛选符合条件的人员：
            // 1. 可用且未退役
            // 2. 拥有该哨位所需的所有技能
            var availablePersonnel = personnel
                .Where(p => p.IsAvailable && !p.IsRetired)
                .Where(p => position.RequiredSkillIds.All(skillId => p.SkillIds.Contains(skillId)))
                .ToList();
            
            position.AvailablePersonnelIds = availablePersonnel.Select(p => p.Id).ToList();
            position.AvailablePersonnelNames = availablePersonnel.Select(p => p.Name).ToList();
        }
    }

    /// <summary>
    /// 导出测试数据到JSON文件（传统方式）
    /// 先生成数据，然后委托给导出器执行文件写入操作
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public async Task ExportToFileAsync(string filePath)
    {
        var exportData = GenerateTestData();
        await _exporter.ExportToFileAsync(exportData, filePath);
    }

    /// <summary>
    /// 导出测试数据到StorageFile（WinUI3方式）
    /// 先生成数据，然后委托给导出器执行StorageFile写入操作
    /// </summary>
    /// <param name="file">StorageFile对象</param>
    public async Task ExportToStorageFileAsync(StorageFile file)
    {
        var exportData = GenerateTestData();
        await _exporter.ExportToStorageFileAsync(exportData, file);
    }

    /// <summary>
    /// 生成测试数据并返回JSON字符串
    /// 先生成数据，然后委托给导出器执行JSON序列化
    /// </summary>
    /// <returns>JSON格式的测试数据</returns>
    public string GenerateTestDataAsJson()
    {
        var exportData = GenerateTestData();
        return _exporter.ExportToJson(exportData);
    }
}
