using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.Models;
using AutoScheduling3.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.Services;

/// <summary>
/// 人员服务实现
/// </summary>
public class PersonnelService : IPersonnelService
{
    private readonly IPersonalRepository _repository;
    private readonly ISkillRepository _skillRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly PersonnelMapper _mapper;

    public PersonnelService(
        IPersonalRepository repository, 
        ISkillRepository skillRepository,
        IPositionRepository positionRepository,
        PersonnelMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<List<PersonnelDto>> GetAllAsync()
    {
        var models = await _repository.GetAllAsync();
        // 修正：确保类型为 List<Personal>，而不是 List<AutoScheduling3.Models.Personal>
        return await _mapper.ToDtoListAsync(models);
    }

    public async Task<PersonnelDto?> GetByIdAsync(int id)
    {
        var model = await _repository.GetByIdAsync(id);
        return model == null ? null : await _mapper.ToDtoAsync(model);
    }

    public async Task<PersonnelDto> CreateAsync(CreatePersonnelDto dto)
    {
        // 验证基础数据
        ValidateCreateDto(dto);
        
        // 验证业务规则
        await ValidateSkillIdsAsync(dto.SkillIds);
        // 移除职位相关验证 - 根据需求3.1

        // 转换并创建
        var model = _mapper.ToModel(dto);
        var id = await _repository.CreateAsync(model);
        
        // 返回创建的对象
        model.Id = id;
        return await _mapper.ToDtoAsync(model);
    }

    public async Task UpdateAsync(int id, UpdatePersonnelDto dto)
    {
        // 验证基础数据
        ValidateUpdateDto(dto);
        
        // 验证业务规则
        await ValidateSkillIdsAsync(dto.SkillIds);
        // 移除职位相关验证 - 根据需求3.1

        // 获取现有对象
        var model = await _repository.GetByIdAsync(id);
        if (model == null)
            throw new InvalidOperationException($"人员 {id} 不存在");

        // 更新并保存
        _mapper.UpdateModel(model, dto);
        await _repository.UpdateAsync(model);
    }

    public async Task DeleteAsync(int id)
    {
        var exists = await _repository.ExistsAsync(id);
        if (!exists)
            throw new InvalidOperationException($"人员 {id} 不存在");

        await _repository.DeleteAsync(id);
    }

    public async Task<List<PersonnelDto>> SearchAsync(string keyword)
    {
        var models = await _repository.SearchByNameAsync(keyword);
        // 修正：确保类型为 List<Personal>，而不是 List<AutoScheduling3.Models.Personal>
        return await _mapper.ToDtoListAsync(models);
    }

    private void ValidateCreateDto(CreatePersonnelDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("姓名为必填项", nameof(dto.Name));

        if (dto.Name.Length > 50)
            throw new ArgumentException("姓名长度不能超过50字符", nameof(dto.Name));

        if (dto.SkillIds == null || dto.SkillIds.Count == 0)
            throw new ArgumentException("至少选择一项技能", nameof(dto.SkillIds));

        if (dto.RecentShiftIntervalCount < 0 || dto.RecentShiftIntervalCount > 999)
            throw new ArgumentException("间隔数必须在0-999之间", nameof(dto.RecentShiftIntervalCount));
    }

    private void ValidateUpdateDto(UpdatePersonnelDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("姓名为必填项", nameof(dto.Name));

        if (dto.Name.Length > 50)
            throw new ArgumentException("姓名长度不能超过50字符", nameof(dto.Name));

        if (dto.SkillIds == null || dto.SkillIds.Count == 0)
            throw new ArgumentException("至少选择一项技能", nameof(dto.SkillIds));
    }

    /// <summary>
    /// 获取可用人员（业务规则：在职且可用）
    /// </summary>
    public async Task<List<PersonnelDto>> GetAvailablePersonnelAsync()
    {
        var allPersonnel = await _repository.GetAllAsync();
        var availablePersonnel = allPersonnel.Where(p => p.IsAvailable && !p.IsRetired).ToList();
        return await _mapper.ToDtoListAsync(availablePersonnel);
    }

    /// <summary>
    /// 验证人员技能匹配（业务规则验证）
    /// </summary>
    public async Task<bool> ValidatePersonnelSkillsAsync(int personnelId, int positionId)
    {
        // 获取人员信息
        var personnel = await _repository.GetByIdAsync(personnelId);
        if (personnel == null)
            return false;

        // 获取哨位信息
        var position = await _positionRepository.GetByIdAsync(positionId);
        if (position == null)
            return false;

        // 检查人员是否可用
        if (!personnel.IsAvailable || personnel.IsRetired)
            return false;

        // 检查技能匹配：人员技能必须包含哨位所需的所有技能
        if (position.RequiredSkillIds == null || position.RequiredSkillIds.Count == 0)
            return true; // 哨位无技能要求

        if (personnel.SkillIds == null || personnel.SkillIds.Count == 0)
            return false; // 人员无技能但哨位有要求

        // 验证人员技能是否包含哨位所需的所有技能
        return position.RequiredSkillIds.All(requiredSkillId => 
            personnel.SkillIds.Contains(requiredSkillId));
    }

    /// <summary>
    /// 获取人员可用的哨位列表 - 根据需求3.1
    /// </summary>
    public async Task<List<PositionDto>> GetAvailablePositionsAsync(int personnelId)
    {
        // 获取人员信息
        var personnel = await _repository.GetByIdAsync(personnelId);
        if (personnel == null)
            throw new InvalidOperationException($"人员 {personnelId} 不存在");

        // 检查人员是否可用
        if (!personnel.IsAvailable || personnel.IsRetired)
            return new List<PositionDto>();

        // 获取所有哨位
        var allPositions = await _positionRepository.GetAllAsync();
        var availablePositions = new List<PositionLocation>();

        // 筛选人员可以胜任的哨位
        foreach (var position in allPositions)
        {
            // 检查哨位的可用人员列表是否包含该人员
            if (position.AvailablePersonnelIds?.Contains(personnelId) == true)
            {
                // 验证技能匹配
                if (await ValidatePersonnelSkillsAsync(personnelId, position.Id))
                {
                    availablePositions.Add(position);
                }
            }
        }

        // 转换为DTO（需要使用PositionMapper）
        var positionMapper = new DTOs.Mappers.PositionMapper(_skillRepository, _repository);
        return await positionMapper.ToDtoListAsync(availablePositions);
    }

    /// <summary>
    /// 验证技能ID是否有效
    /// </summary>
    private async Task ValidateSkillIdsAsync(List<int> skillIds)
    {
        if (skillIds == null || skillIds.Count == 0)
            throw new ArgumentException("至少选择一项技能");

        // 检查技能是否存在
        var existingSkills = await _skillRepository.GetByIdsAsync(skillIds);
        var existingSkillIds = existingSkills.Select(s => s.Id).ToList();
        
        var invalidSkillIds = skillIds.Except(existingSkillIds).ToList();
        if (invalidSkillIds.Any())
            throw new ArgumentException($"技能ID无效: {string.Join(", ", invalidSkillIds)}");
    }


}
