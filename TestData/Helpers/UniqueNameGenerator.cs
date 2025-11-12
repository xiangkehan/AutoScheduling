using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.TestData.Helpers
{
    /// <summary>
    /// 唯一名称生成器
    /// </summary>
    public class UniqueNameGenerator
    {
        private readonly Random _random;

        public UniqueNameGenerator(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// 生成唯一名称
        /// </summary>
        /// <param name="availableNames">可用的预定义名称列表（会被修改）</param>
        /// <param name="usedNames">已使用的名称集合</param>
        /// <param name="fallbackPrefix">备用名称前缀</param>
        /// <param name="index">当前索引</param>
        /// <returns>唯一的名称</returns>
        /// <exception cref="InvalidOperationException">当无法生成唯一名称时抛出</exception>
        public string Generate(
            List<string> availableNames,
            HashSet<string> usedNames,
            string fallbackPrefix,
            int index)
        {
            if (availableNames == null)
                throw new ArgumentNullException(nameof(availableNames));
            if (usedNames == null)
                throw new ArgumentNullException(nameof(usedNames));
            if (string.IsNullOrWhiteSpace(fallbackPrefix))
                throw new ArgumentException("Fallback prefix cannot be null or whitespace", nameof(fallbackPrefix));

            // 尝试从预定义列表中选择名称
            if (availableNames.Count > 0)
            {
                // 随机选择一个可用名称
                int randomIndex = _random.Next(availableNames.Count);
                string selectedName = availableNames[randomIndex];
                availableNames.RemoveAt(randomIndex);

                // 确保名称唯一
                if (!usedNames.Contains(selectedName))
                {
                    usedNames.Add(selectedName);
                    return selectedName;
                }
            }

            // 使用备用名称生成逻辑（前缀+索引）
            string fallbackName = $"{fallbackPrefix}{index}";
            int attempt = 0;
            const int maxAttempts = 1000;

            while (usedNames.Contains(fallbackName) && attempt < maxAttempts)
            {
                attempt++;
                fallbackName = $"{fallbackPrefix}{index}_{attempt}";
            }

            if (usedNames.Contains(fallbackName))
            {
                // 生成详细的错误报告
                throw new InvalidOperationException(
                    $"无法生成唯一名称。" +
                    $"\n前缀: {fallbackPrefix}" +
                    $"\n索引: {index}" +
                    $"\n尝试次数: {maxAttempts}" +
                    $"\n已使用名称数量: {usedNames.Count}" +
                    $"\n可用预定义名称数量: {availableNames.Count}" +
                    $"\n建议: 增加预定义名称列表或减少生成数量");
            }

            usedNames.Add(fallbackName);
            return fallbackName;
        }
    }
}
