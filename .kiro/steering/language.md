# Language Preference

## Communication Language

- **Primary Language**: 中文（Chinese）
- Use Chinese for all communications, explanations, and discussions
- Code comments should be in Chinese when appropriate
- Documentation and README files should be in Chinese
- Git commit messages should be in Chinese

## Code Standards

- Variable names, function names, and class names: Use English (following C# conventions)
- Comments: Use Chinese for better understanding
- UI text and labels: Use Chinese
- Error messages: Use Chinese

## Git Commit Messages

- Use Chinese for commit messages
- Follow conventional commit format with Chinese descriptions
- Example: `feat: 添加人员选择功能` or `fix: 修复排班算法bug`

## Example

```csharp
// 获取所有可用的人员
public async Task<List<PersonnelDto>> GetAvailablePersonnelAsync()
{
    // 从数据库查询人员
    var personnel = await _repository.GetAllAsync();
    
    // 过滤出可用的人员
    return personnel.Where(p => p.IsAvailable).ToList();
}
```
