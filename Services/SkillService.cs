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
    private readonly SkillMapper _mapper;

    public SkillService(ISkillRepository repository, SkillMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
        ValidateCreateDto(dto);

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

        ValidateUpdateDto(dto);

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
}
