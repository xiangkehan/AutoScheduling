using AutoScheduling3.Tests;

namespace AutoScheduling3.TestData;

/// <summary>
/// 简单测试运行器 - 用于验证测试数据生成器
/// </summary>
public static class TestRunner
{
    /// <summary>
    /// 运行基础测试
    /// </summary>
    public static void RunBasicTests()
    {
        TestDataGeneratorBasicTests.RunAllTests();
    }
}
