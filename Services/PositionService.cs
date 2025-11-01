using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.Services.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace AutoScheduling3.Services;

/// <summary>
/// 哨位服务实现
/// </summary>
public class PositionService : IPositionService
{
    private readonly IPositionRepository _repository;
    private readonly ISkillRepository _skillRepository;
    private readonly PositionMapper _mapper;

    public PositionService(
        IPositionRepository repository, 
        ISkillRepository skillRepository,
        PositionMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// 获取所有哨位
    /// </summary>
    public async Task<List<PositionDto>> GetAllAsync()
    {
        var positions = await _repository.GetAllAsync();
        return await _mapper.ToDtoListAsync(positions);
    }

    /// <summary>
    /// 根据 ID 获取哨位
    /// </summary>
    public async Task<PositionDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("无效的哨位ID", nameof(id));

        var position = await _repository.GetByIdAsync(id);
        return position != null ? await _mapper.ToDtoAsync(position) : null;
    }

    /// <summary>
    /// 创建新哨位
    /// </summary>
    public async Task<PositionDto> CreateAsync(CreatePositionDto dto)
    {
        // 基础验证
        ValidateCreateDto(dto);
        
        // 业务规则验证
        await ValidateSkillRequirementsAsync(dto.RequiredSkillIds);

        var model = _mapper.ToModel(dto);
        var id = await _repository.CreateAsync(model);
        
        model.Id = id;
        return await _mapper.ToDtoAsync(model);
    }

    /// <summary>
    /// 更新哨位
    /// </summary>
    public async Task UpdateAsync(int id, UpdatePositionDto dto)
    {
        if (id <= 0)
            throw new ArgumentException("无效的哨位ID", nameof(id));

        // 基础验证
        ValidateUpdateDto(dto);
        
        // 业务规则验证
        await ValidateSkillRequirementsAsync(dto.RequiredSkillIds);

        var existingPosition = await _repository.GetByIdAsync(id);
        if (existingPosition == null)
            throw new ArgumentException($"哨位 ID {id} 不存在", nameof(id));

        _mapper.UpdateModel(existingPosition, dto);
        await _repository.UpdateAsync(existingPosition);
    }

    /// <summary>
    /// 删除哨位
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("无效的哨位ID", nameof(id));

        var exists = await _repository.ExistsAsync(id);
        if (!exists)
            throw new ArgumentException($"哨位 ID {id} 不存在", nameof(id));

        await _repository.DeleteAsync(id);
    }

    /// <summary>
    /// 获取活跃的哨位列表
    /// </summary>
    public async Task<List<PositionDto>> GetActiveAsync()
    {
        var allPositions = await _repository.GetAllAsync();
        return await _mapper.ToDtoListAsync(allPositions);
    }

    /// <summary>
    /// 按优先级排序获取哨位
    /// </summary>
    public async Task<List<PositionDto>> GetByPriorityAsync()
    {
        var positions = await _repository.GetAllAsync();
        return await _mapper.ToDtoListAsync(positions);
    }

    /// <summary>
    /// 搜索哨位
    /// </summary>
    public async Task<List<PositionDto>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetAllAsync();

        var filteredPositions = await _repository.SearchByNameAsync(keyword);
        return await _mapper.ToDtoListAsync(filteredPositions);
    }

    /// <summary>
    /// 验证创建 DTO
    /// </summary>
    private void ValidateCreateDto(CreatePositionDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("哨位名称为必填项", nameof(dto.Name));

        if (dto.Name.Length > 100)
            throw new ArgumentException("哨位名称不能超过100个字符", nameof(dto.Name));

        if (string.IsNullOrWhiteSpace(dto.Location))
            throw new ArgumentException("哨位地点为必填项", nameof(dto.Location));

        if (dto.Location.Length > 200)
            throw new ArgumentException("地点不能超过200个字符", nameof(dto.Location));

        if (dto.Description != null && dto.Description.Length > 500)
            throw new ArgumentException("介绍不能超过500个字符", nameof(dto.Description));

        if (dto.Requirements != null && dto.Requirements.Length > 1000)
            throw new ArgumentException("要求说明不能超过1000个字符", nameof(dto.Requirements));

        if (dto.RequiredSkillIds == null || dto.RequiredSkillIds.Count == 0)
            throw new ArgumentException("哨位必须至少需要一项技能", nameof(dto.RequiredSkillIds));
    }

    /// <summary>
    /// 验证更新 DTO
    /// </summary>
    private void ValidateUpdateDto(UpdatePositionDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("哨位名称为必填项", nameof(dto.Name));

        if (dto.Name.Length > 100)
            throw new ArgumentException("哨位名称不能超过100个字符", nameof(dto.Name));

        if (string.IsNullOrWhiteSpace(dto.Location))
            throw new ArgumentException("哨位地点为必填项", nameof(dto.Location));

        if (dto.Location.Length > 200)
            throw new ArgumentException("地点不能超过200个字符", nameof(dto.Location));

        if (dto.Description != null && dto.Description.Length > 500)
            throw new ArgumentException("介绍不能超过500个字符", nameof(dto.Description));

        if (dto.Requirements != null && dto.Requirements.Length > 1000)
            throw new ArgumentException("要求说明不能超过1000个字符", nameof(dto.Requirements));

        if (dto.RequiredSkillIds == null || dto.RequiredSkillIds.Count == 0)
            throw new ArgumentException("哨位必须至少需要一项技能", nameof(dto.RequiredSkillIds));
    }

    /// <summary>
    /// 验证哨位数据完整性（业务规则）
    /// </summary>
    public async Task<bool> ValidatePositionDataAsync(PositionDto position)
    {
        if (position == null)
            return false;

        // 验证基础数据
        if (string.IsNullOrWhiteSpace(position.Name) || 
            string.IsNullOrWhiteSpace(position.Location))
            return false;

        // 验证技能要求
        if (position.RequiredSkillIds == null || position.RequiredSkillIds.Count == 0)
            return false;

        // 验证技能ID是否存在
        try
        {
            await ValidateSkillRequirementsAsync(position.RequiredSkillIds);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证技能要求（业务规则验证）
    /// </summary>
    private async Task ValidateSkillRequirementsAsync(List<int> requiredSkillIds)
    {
        if (requiredSkillIds == null || requiredSkillIds.Count == 0)
            throw new ArgumentException("哨位必须至少需要一项技能");

        // 检查技能是否存在
        var existingSkills = await _skillRepository.GetByIdsAsync(requiredSkillIds);
        var existingSkillIds = existingSkills.Select(s => s.Id).ToList();
        
        var invalidSkillIds = requiredSkillIds.Except(existingSkillIds).ToList();
        if (invalidSkillIds.Any())
            throw new ArgumentException($"技能ID无效: {string.Join(", ", invalidSkillIds)}");

        // 检查是否有重复的技能ID
        var duplicateSkillIds = requiredSkillIds.GroupBy(id => id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        
        if (duplicateSkillIds.Any())
            throw new ArgumentException($"技能ID重复: {string.Join(", ", duplicateSkillIds)}");
    }
}
