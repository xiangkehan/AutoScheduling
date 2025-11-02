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

    /// <summary>
    /// 添加可用人员到哨位 - 根据需求3.2
    /// </summary>
    public async Task AddAvailablePersonnelAsync(int positionId, int personnelId)
    {
        if (positionId <= 0)
            throw new ArgumentException("无效的哨位ID", nameof(positionId));
        
        if (personnelId <= 0)
            throw new ArgumentException("无效的人员ID", nameof(personnelId));

        // 获取哨位信息
        var position = await _repository.GetByIdAsync(positionId);
        if (position == null)
            throw new ArgumentException($"哨位 ID {positionId} 不存在", nameof(positionId));

        // 验证人员是否存在（通过Repository接口）
        var personnelExists = await _repository.PersonnelExistsAsync(personnelId);
        if (!personnelExists)
            throw new ArgumentException($"人员 ID {personnelId} 不存在", nameof(personnelId));

        // 验证人员技能是否匹配哨位要求
        await ValidatePersonnelSkillsAsync(personnelId, positionId);

        // 添加人员到可用列表（如果尚未存在）
        await _repository.AddAvailablePersonnelAsync(positionId, personnelId);
    }

    /// <summary>
    /// 从哨位移除可用人员 - 根据需求3.2
    /// </summary>
    public async Task RemoveAvailablePersonnelAsync(int positionId, int personnelId)
    {
        if (positionId <= 0)
            throw new ArgumentException("无效的哨位ID", nameof(positionId));
        
        if (personnelId <= 0)
            throw new ArgumentException("无效的人员ID", nameof(personnelId));

        // 验证哨位是否存在
        var positionExists = await _repository.ExistsAsync(positionId);
        if (!positionExists)
            throw new ArgumentException($"哨位 ID {positionId} 不存在", nameof(positionId));

        // 移除人员从可用列表
        await _repository.RemoveAvailablePersonnelAsync(positionId, personnelId);
    }

    /// <summary>
    /// 获取哨位的可用人员列表 - 根据需求3.2
    /// </summary>
    public async Task<List<PersonnelDto>> GetAvailablePersonnelAsync(int positionId)
    {
        if (positionId <= 0)
            throw new ArgumentException("无效的哨位ID", nameof(positionId));

        // 验证哨位是否存在
        var position = await _repository.GetByIdAsync(positionId);
        if (position == null)
            throw new ArgumentException($"哨位 ID {positionId} 不存在", nameof(positionId));

        // 获取可用人员ID列表
        var availablePersonnelIds = await _repository.GetAvailablePersonnelIdsAsync(positionId);
        if (!availablePersonnelIds.Any())
            return new List<PersonnelDto>();

        // 获取人员详细信息（通过Repository接口）
        var personnel = await _repository.GetPersonnelByIdsAsync(availablePersonnelIds);
        
        // 转换为DTO（需要使用PersonnelMapper）
        var personnelMapper = new DTOs.Mappers.PersonnelMapper(_skillRepository, _repository);
        return await personnelMapper.ToDtoListAsync(personnel);
    }

    /// <summary>
    /// 验证人员技能是否满足哨位要求 - 根据需求3.2
    /// </summary>
    public async Task<bool> ValidatePersonnelSkillsAsync(int personnelId, int positionId)
    {
        if (personnelId <= 0 || positionId <= 0)
            return false;

        // 获取哨位信息
        var position = await _repository.GetByIdAsync(positionId);
        if (position == null)
            return false;

        // 获取人员信息（通过Repository接口）
        var personnel = await _repository.GetPersonnelByIdAsync(personnelId);
        if (personnel == null)
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
}
