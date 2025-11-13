using System.Collections.Generic;

namespace AutoScheduling3.TestData.Generators
{
    /// <summary>
    /// 实体生成器基础接口
    /// </summary>
    /// <typeparam name="T">要生成的实体类型</typeparam>
    public interface IEntityGenerator<T>
    {
        /// <summary>
        /// 生成实体数据
        /// </summary>
        /// <param name="dependencies">生成数据所需的依赖项</param>
        /// <returns>生成的实体列表</returns>
        List<T> Generate(params object[] dependencies);
    }
}
