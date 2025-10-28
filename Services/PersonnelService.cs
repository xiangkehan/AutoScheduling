using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Services;

/// <summary>
/// 人员服务实现
/// </summary>
public class PersonnelService : IPersonnelService
{
    private readonly IPersonalRepository _repository;
    private readonly PersonnelMapper _mapper;

    public PersonnelService(IPersonalRepository repository, PersonnelMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
        // 验证
        ValidateCreateDto(dto);

        // 转换并创建
        var model = _mapper.ToModel(dto);
        var id = await _repository.CreateAsync(model);
        
        // 返回创建的对象
        model.Id = id;
        return await _mapper.ToDtoAsync(model);
    }

    public async Task UpdateAsync(int id, UpdatePersonnelDto dto)
    {
        // 验证
        ValidateUpdateDto(dto);

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
}
