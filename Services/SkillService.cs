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
/// 技能服务实现
/// </summary>
public class SkillService : ISkillService
{
    private readonly ISkillRepository _repository;
    private readonly IPersonalRepository _personnelRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly SkillMapper _mapper;

    public SkillService(
        ISkillRepository repository, 
        IPersonalRepository personnelRepository,
        IPositionRepository positionRepository,
        SkillMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _personnelRepository = personnelRepository ?? throw new ArgumentNullException(nameof(personnelRepository));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// 获取所有技能
    /// </summary>
    public async Task<List<SkillDto>> GetAllAsync()
    {
        var skills = await _repository.GetAllAsync();
        return _mapper.ToDtoList(skills);
    }

    /// <summary>
    /// 根据 ID 获取技能
    /// </summary>
    public async Task<SkillDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("无效的技能ID", nameof(id));

        var skill = await _repository.GetByIdAsync(id);
        return skill != null ? _mapper.ToDto(skill) : null;
    }

    /// <summary>
    /// 创建新技能
    /// </summary>
    public async Task<SkillDto> CreateAsync(CreateSkillDto dto)
    {
        // 基础验证
        ValidateCreateDto(dto);
        
        // 业务规则验证：检查名称唯一性
        await ValidateSkillNameUniquenessAsync(dto.Name);

        var model = _mapper.ToModel(dto);
        var id = await _repository.CreateAsync(model);
        
        model.Id = id;
        return _mapper.ToDto(model);
    }

    /// <summary>
    /// 更新技能
    /// </summary>
    public async Task UpdateAsync(int id, UpdateSkillDto dto)
    {
        if (id <= 0)
            throw new ArgumentException("无效的技能ID", nameof(id));

        // 基础验证
        ValidateUpdateDto(dto);
        
        // 业务规则验证：检查名称唯一性（排除当前记录）
        await ValidateSkillNameUniquenessAsync(dto.Name, id);

        var existingSkill = await _repository.GetByIdAsync(id);
        if (existingSkill == null)
            throw new ArgumentException($"技能 ID {id} 不存在", nameof(id));

        _mapper.UpdateModel(existingSkill, dto);
        await _repository.UpdateAsync(existingSkill);
    }

    /// <summary>
    /// 删除技能
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("无效的技能ID", nameof(id));

        var exists = await _repository.ExistsAsync(id);
        if (!exists)
            throw new ArgumentException($"技能 ID {id} 不存在", nameof(id));

        // 业务规则验证：检查技能是否被使用
        await ValidateSkillNotInUseAsync(id);

        await _repository.DeleteAsync(id);
    }

    /// <summary>
    /// 获取活跃的技能列表
    /// </summary>
    public async Task<List<SkillDto>> GetActiveAsync()
    {
        var allSkills = await _repository.GetAllAsync();
        var activeSkills = allSkills.Where(s => s.IsActive).ToList();
        return _mapper.ToDtoList(activeSkills);
    }

    /// <summary>
    /// 搜索技能
    /// </summary>
    public async Task<List<SkillDto>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetAllAsync();

        var allSkills = await _repository.GetAllAsync();
        var filteredSkills = allSkills.Where(s => 
            s.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            (s.Description != null && s.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        return _mapper.ToDtoList(filteredSkills);
    }

    /// <summary>
    /// 根据 ID 列表批量获取技能
    /// </summary>
    public async Task<List<SkillDto>> GetByIdsAsync(List<int> ids)
    {
        if (ids == null || ids.Count == 0)
            return new List<SkillDto>();

        var allSkills = await _repository.GetAllAsync();
        var matchedSkills = allSkills.Where(s => ids.Contains(s.Id)).ToList();
        return _mapper.ToDtoList(matchedSkills);
    }

    /// <summary>
    /// 验证创建 DTO
    /// </summary>
    private void ValidateCreateDto(CreateSkillDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("技能名称为必填项", nameof(dto.Name));

        if (dto.Name.Length > 50)
            throw new ArgumentException("技能名称不能超过50个字符", nameof(dto.Name));

        if (dto.Description != null && dto.Description.Length > 500)
            throw new ArgumentException("技能描述不能超过500个字符", nameof(dto.Description));
    }

    /// <summary>
    /// 验证更新 DTO
    /// </summary>
    private void ValidateUpdateDto(UpdateSkillDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("技能名称为必填项", nameof(dto.Name));

        if (dto.Name.Length > 50)
            throw new ArgumentException("技能名称不能超过50个字符", nameof(dto.Name));

        if (dto.Description != null && dto.Description.Length > 500)
            throw new ArgumentException("技能描述不能超过500个字符", nameof(dto.Description));
    }

    /// <summary>
    /// 验证技能名称唯一性（业务规则验证）
    /// </summary>
    private async Task ValidateSkillNameUniquenessAsync(string name, int? excludeId = null)
    {
        var nameExists = await _repository.NameExistsAsync(name, excludeId);
        if (nameExists)
            throw new ArgumentException($"技能名称 '{name}' 已存在");
    }

    /// <summary>
    /// 验证技能是否被使用（业务规则验证）
    /// </summary>
    private async Task ValidateSkillNotInUseAsync(int skillId)
    {
        // 检查是否有人员使用此技能
        var allPersonnel = await _personnelRepository.GetAllAsync();
        var personnelUsingSkill = allPersonnel.Where(p => 
            p.SkillIds != null && p.SkillIds.Contains(skillId)).ToList();
        
        if (personnelUsingSkill.Any())
        {
            var personnelNames = string.Join(", ", personnelUsingSkill.Select(p => p.Name));
            throw new InvalidOperationException($"无法删除技能，以下人员正在使用此技能: {personnelNames}");
        }

        // 检查是否有哨位需要此技能
        var allPositions = await _positionRepository.GetAllAsync();
        var positionsRequiringSkill = allPositions.Where(p => 
            p.RequiredSkillIds != null && p.RequiredSkillIds.Contains(skillId)).ToList();
        
        if (positionsRequiringSkill.Any())
        {
            var positionNames = string.Join(", ", positionsRequiringSkill.Select(p => p.Name));
            throw new InvalidOperationException($"无法删除技能，以下哨位需要此技能: {positionNames}");
        }
    }

    /// <summary>
    /// 检查技能是否被使用（不抛出异常，返回使用情况）
    /// </summary>
    public async Task<SkillUsageInfo> CheckSkillUsageAsync(int skillId)
    {
        var usageInfo = new SkillUsageInfo { SkillId = skillId };

        // 检查人员使用情况
        var allPersonnel = await _personnelRepository.GetAllAsync();
        usageInfo.UsedByPersonnel = allPersonnel
            .Where(p => p.SkillIds != null && p.SkillIds.Contains(skillId))
            .Select(p => new { p.Id, p.Name })
            .Cast<object>()
            .ToList();

        // 检查哨位使用情况
        var allPositions = await _positionRepository.GetAllAsync();
        usageInfo.RequiredByPositions = allPositions
            .Where(p => p.RequiredSkillIds != null && p.RequiredSkillIds.Contains(skillId))
            .Select(p => new { p.Id, p.Name })
            .Cast<object>()
            .ToList();

        usageInfo.IsInUse = usageInfo.UsedByPersonnel.Any() || usageInfo.RequiredByPositions.Any();
        
        return usageInfo;
    }
}

/// <summary>
/// 技能使用情况信息
/// </summary>
public class SkillUsageInfo
{
    public int SkillId { get; set; }
    public bool IsInUse { get; set; }
    public List<object> UsedByPersonnel { get; set; } = new();
    public List<object> RequiredByPositions { get; set; } = new();
}
