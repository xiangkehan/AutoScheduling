using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 服务基础接口 - 定义通用的业务逻辑操作
/// </summary>
/// <typeparam name="TDto">数据传输对象类型</typeparam>
public interface IService<TDto> where TDto : class
{
    /// <summary>
    /// 获取所有数据
    /// </summary>
    Task<List<TDto>> GetAllAsync();

    /// <summary>
    /// 根据ID获取数据
    /// </summary>
    Task<TDto?> GetByIdAsync(int id);

    /// <summary>
    /// 删除数据
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 搜索数据
    /// </summary>
    Task<List<TDto>> SearchAsync(string keyword);
}

/// <summary>
/// 可创建服务接口 - 支持创建操作的服务
/// </summary>
/// <typeparam name="TDto">数据传输对象类型</typeparam>
/// <typeparam name="TCreateDto">创建数据传输对象类型</typeparam>
public interface ICreatableService<TDto, TCreateDto> : IService<TDto> 
    where TDto : class 
    where TCreateDto : class
{
    /// <summary>
    /// 创建数据
    /// </summary>
    Task<TDto> CreateAsync(TCreateDto createDto);
}

/// <summary>
/// 可更新服务接口 - 支持更新操作的服务
/// </summary>
/// <typeparam name="TDto">数据传输对象类型</typeparam>
/// <typeparam name="TUpdateDto">更新数据传输对象类型</typeparam>
public interface IUpdatableService<TDto, TUpdateDto> : IService<TDto> 
    where TDto : class 
    where TUpdateDto : class
{
    /// <summary>
    /// 更新数据
    /// </summary>
    Task UpdateAsync(int id, TUpdateDto updateDto);
}

/// <summary>
/// 完整CRUD服务接口 - 支持完整增删改查操作的服务
/// </summary>
/// <typeparam name="TDto">数据传输对象类型</typeparam>
/// <typeparam name="TCreateDto">创建数据传输对象类型</typeparam>
/// <typeparam name="TUpdateDto">更新数据传输对象类型</typeparam>
public interface ICrudService<TDto, TCreateDto, TUpdateDto> : 
    ICreatableService<TDto, TCreateDto>, 
    IUpdatableService<TDto, TUpdateDto>
    where TDto : class 
    where TCreateDto : class 
    where TUpdateDto : class
{
}