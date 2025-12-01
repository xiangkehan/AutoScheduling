using AutoScheduling3.DTOs;

namespace AutoScheduling3.TestData.Helpers;

/// <summary>
/// 技能共现分析器，用于分析哨位技能需求的共现模式
/// </summary>
internal class SkillCooccurrenceAnalyzer
{
    private Dictionary<(int, int), int> _cooccurrenceMatrix;

    /// <summary>
    /// 初始化技能共现分析器
    /// </summary>
    public SkillCooccurrenceAnalyzer()
    {
        _cooccurrenceMatrix = new Dictionary<(int, int), int>();
    }

    /// <summary>
    /// 构建技能共现矩阵
    /// </summary>
    /// <param name="positions">哨位列表</param>
    /// <returns>技能共现矩阵（技能ID对 -> 共现次数）</returns>
    public Dictionary<(int, int), int> BuildCooccurrenceMatrix(List<PositionDto> positions)
    {
        _cooccurrenceMatrix.Clear();

        // 遍历所有哨位
        foreach (var position in positions)
        {
            var skillIds = position.RequiredSkillIds;
            
            // 如果哨位只需要一个技能，跳过
            if (skillIds.Count < 2)
                continue;

            // 计算该哨位中所有技能对的共现
            for (int i = 0; i < skillIds.Count; i++)
            {
                for (int j = i + 1; j < skillIds.Count; j++)
                {
                    var skill1 = skillIds[i];
                    var skill2 = skillIds[j];

                    // 确保较小的ID在前，保持一致性
                    var key = skill1 < skill2 ? (skill1, skill2) : (skill2, skill1);

                    // 增加共现计数
                    if (_cooccurrenceMatrix.ContainsKey(key))
                    {
                        _cooccurrenceMatrix[key]++;
                    }
                    else
                    {
                        _cooccurrenceMatrix[key] = 1;
                    }
                }
            }
        }

        return _cooccurrenceMatrix;
    }

    /// <summary>
    /// 获取与指定技能集合高频共现的技能
    /// </summary>
    /// <param name="existingSkillIds">现有技能ID集合</param>
    /// <param name="allSkills">所有技能列表</param>
    /// <param name="topN">返回前N个高频技能，默认3个</param>
    /// <returns>高频共现的技能列表，按共现频率降序排列</returns>
    public List<SkillDto> GetCooccurringSkills(
        HashSet<int> existingSkillIds,
        List<SkillDto> allSkills,
        int topN = 3)
    {
        // 如果没有现有技能或共现矩阵为空，返回空列表
        if (existingSkillIds.Count == 0 || _cooccurrenceMatrix.Count == 0)
            return new List<SkillDto>();

        // 统计每个候选技能与现有技能集合的总共现次数
        var candidateScores = new Dictionary<int, int>();

        foreach (var kvp in _cooccurrenceMatrix)
        {
            var (skill1, skill2) = kvp.Key;
            var cooccurrenceCount = kvp.Value;

            // 检查技能对中是否有一个在现有技能集合中
            if (existingSkillIds.Contains(skill1) && !existingSkillIds.Contains(skill2))
            {
                // skill2 是候选技能
                if (candidateScores.ContainsKey(skill2))
                {
                    candidateScores[skill2] += cooccurrenceCount;
                }
                else
                {
                    candidateScores[skill2] = cooccurrenceCount;
                }
            }
            else if (existingSkillIds.Contains(skill2) && !existingSkillIds.Contains(skill1))
            {
                // skill1 是候选技能
                if (candidateScores.ContainsKey(skill1))
                {
                    candidateScores[skill1] += cooccurrenceCount;
                }
                else
                {
                    candidateScores[skill1] = cooccurrenceCount;
                }
            }
        }

        // 如果没有候选技能，返回空列表
        if (candidateScores.Count == 0)
            return new List<SkillDto>();

        // 按共现频率降序排序，取前topN个
        var topSkillIds = candidateScores
            .OrderByDescending(kvp => kvp.Value)
            .Take(topN)
            .Select(kvp => kvp.Key)
            .ToList();

        // 将技能ID转换为SkillDto对象
        var result = allSkills
            .Where(skill => topSkillIds.Contains(skill.Id))
            .ToList();

        return result;
    }

    /// <summary>
    /// 获取共现矩阵的副本（用于调试和测试）
    /// </summary>
    /// <returns>共现矩阵的副本</returns>
    public Dictionary<(int, int), int> GetCooccurrenceMatrix()
    {
        return new Dictionary<(int, int), int>(_cooccurrenceMatrix);
    }

    /// <summary>
    /// 获取指定技能对的共现次数
    /// </summary>
    /// <param name="skillId1">技能1的ID</param>
    /// <param name="skillId2">技能2的ID</param>
    /// <returns>共现次数，如果不存在则返回0</returns>
    public int GetCooccurrenceCount(int skillId1, int skillId2)
    {
        var key = skillId1 < skillId2 ? (skillId1, skillId2) : (skillId2, skillId1);
        return _cooccurrenceMatrix.TryGetValue(key, out var count) ? count : 0;
    }

    /// <summary>
    /// 获取所有与指定技能共现的技能及其共现次数
    /// </summary>
    /// <param name="skillId">技能ID</param>
    /// <returns>共现技能ID和次数的字典</returns>
    public Dictionary<int, int> GetCooccurringSkillsWithCounts(int skillId)
    {
        var result = new Dictionary<int, int>();

        foreach (var kvp in _cooccurrenceMatrix)
        {
            var (skill1, skill2) = kvp.Key;
            var count = kvp.Value;

            if (skill1 == skillId)
            {
                result[skill2] = count;
            }
            else if (skill2 == skillId)
            {
                result[skill1] = count;
            }
        }

        return result;
    }

    /// <summary>
    /// 清空共现矩阵
    /// </summary>
    public void Clear()
    {
        _cooccurrenceMatrix.Clear();
    }
}
