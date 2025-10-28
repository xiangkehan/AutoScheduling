using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.Services.Interfaces;

namespace AutoScheduling3.Services;

/// <summary>
/// 哨位服务实现
/// </summary>
public class PositionService : IPositionService
{
    private readonly IPositionRepository _repository;
    private readonly PositionMapper _mapper;

    public PositionService(IPositionRepository repository, PositionMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// 获取所有哨位
    /// </summary>
    public async Task<List<PositionDto>> GetAllAsync()
    {
        var positions = await _repository.GetAllAsync();
        return _mapper.ToDtoList(positions);
    }

    /// <summary>
    /// 根据 ID 获取哨位
    /// </summary>
    public async Task<PositionDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("无效的哨位ID", nameof(id));

        var position = await _repository.GetByIdAsync(id);
        return position != null ? _mapper.ToDto(position) : null;
    }

    /// <summary>
    /// 创建新哨位
    /// </summary>
    public async Task<PositionDto> CreateAsync(CreatePositionDto dto)
    {
        ValidateCreateDto(dto);

        var model = _mapper.ToModel(dto);
        var id = await _repository.CreateAsync(model);
        
        model.Id = id;
        return _mapper.ToDto(model);
    }

    /// <summary>
    /// 更新哨位
    /// </summary>
    public async Task UpdateAsync(int id, UpdatePositionDto dto)
    {
        if (id <= 0)
            throw new ArgumentException("无效的哨位ID", nameof(id));

        ValidateUpdateDto(dto);

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
        var activePositions = allPositions.Where(p => p.IsActive).ToList();
        return _mapper.ToDtoList(activePositions);
    }

    /// <summary>
    /// 按优先级排序获取哨位
    /// </summary>
    public async Task<List<PositionDto>> GetByPriorityAsync()
    {
        var positions = await _repository.GetAllAsync();
        var sortedPositions = positions.OrderBy(p => p.Priority).ToList();
        return _mapper.ToDtoList(sortedPositions);
    }

    /// <summary>
    /// 搜索哨位
    /// </summary>
    public async Task<List<PositionDto>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetAllAsync();

        var allPositions = await _repository.GetAllAsync();
        var filteredPositions = allPositions.Where(p => 
            p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            (p.Description != null && p.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        return _mapper.ToDtoList(filteredPositions);
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

        if (dto.RequiredPersonnel < 1)
            throw new ArgumentException("所需人数必须至少为1", nameof(dto.RequiredPersonnel));

        if (dto.RequiredPersonnel > 50)
            throw new ArgumentException("所需人数不能超过50", nameof(dto.RequiredPersonnel));

        if (dto.Priority < 1)
            throw new ArgumentException("优先级必须至少为1", nameof(dto.Priority));
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

        if (dto.RequiredPersonnel < 1)
            throw new ArgumentException("所需人数必须至少为1", nameof(dto.RequiredPersonnel));

        if (dto.RequiredPersonnel > 50)
            throw new ArgumentException("所需人数不能超过50", nameof(dto.RequiredPersonnel));

        if (dto.Priority < 1)
            throw new ArgumentException("优先级必须至少为1", nameof(dto.Priority));
    }
}
