# Task 7: 快速测试指南

## 目的
验证设置界面正确显示使用 ApplicationData.Current.LocalFolder 的存储路径。

## 快速测试步骤

### 1. 启动应用 (30秒)
```
1. 启动 AutoScheduling3 应用程序
2. 点击导航栏的"设置"按钮
3. 滚动到页面底部的"存储文件路径"部分
```

### 2. 验证路径显示 (1分钟)
检查显示的路径是否符合以下格式：

**数据库文件路径应该类似于：**
```
C:\Users\[用户名]\AppData\Local\Packages\[PackageFamilyName]\LocalState\GuardDutyScheduling.db
```

**配置文件路径应该类似于：**
```
C:\Users\[用户名]\AppData\Local\Packages\[PackageFamilyName]\LocalState\Settings\config.json
```

✅ **关键验证点：**
- [ ] 路径包含 `LocalState`（这是 ApplicationData.Current.LocalFolder 的实际位置）
- [ ] 数据库文件直接在 LocalState 根目录
- [ ] 配置文件在 LocalState\Settings 子目录
- [ ] 路径**不包含**旧的 "AutoScheduling3" 子文件夹

### 3. 测试复制路径 (30秒)
```
1. 点击数据库文件的"复制路径"按钮
2. 打开记事本（Win+R，输入 notepad）
3. 粘贴（Ctrl+V）
4. 验证粘贴的路径与界面显示的一致
```

### 4. 测试打开目录 (30秒)
```
1. 点击数据库文件的"打开目录"按钮
2. 应该打开文件资源管理器
3. 应该定位到 LocalState 文件夹
4. 应该选中 GuardDutyScheduling.db 文件（如果存在）
```

### 5. 测试刷新功能 (15秒)
```
1. 点击"刷新信息"按钮
2. 应该看到短暂的加载指示器
3. 文件信息应该更新
```

## 预期结果

### ✅ 成功标准
- 所有路径都包含 `LocalState`
- 路径不包含旧的 "AutoScheduling3" 子文件夹
- 复制路径功能正常工作
- 打开目录功能正常工作
- 文件信息显示准确（大小、修改时间）

### ❌ 失败标准
- 路径仍然包含 "AutoScheduling3" 子文件夹
- 路径不包含 `LocalState`
- 复制或打开目录功能不工作
- 显示错误消息

## 常见问题

### Q: 文件显示"不存在"是正常的吗？
A: 是的，如果是首次运行应用，某些文件可能还没有创建。这是正常的。

### Q: LocalState 文件夹在哪里？
A: 通常在 `%LocalAppData%\Packages\[PackageFamilyName]\LocalState`

在开发环境中，可能在项目的 bin 目录下。

### Q: 如何确认路径是正确的？
A: 使用"复制路径"功能，然后在文件资源管理器的地址栏粘贴，看是否能找到该位置。

## 测试时间
总计约 3 分钟

## 报告问题
如果发现任何问题，请记录：
1. 显示的路径（截图或复制）
2. 预期的路径
3. 错误消息（如果有）
4. 重现步骤

## 完成
- [ ] 所有测试步骤已完成
- [ ] 所有预期结果已验证
- [ ] 没有发现问题，或问题已记录
