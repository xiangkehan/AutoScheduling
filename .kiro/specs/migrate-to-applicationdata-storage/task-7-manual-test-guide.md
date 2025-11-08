# Task 7 Manual Testing Guide

## 目标
验证设置界面正确显示使用 ApplicationData.Current.LocalFolder 的存储路径

## 测试前准备

### 1. 构建并运行应用程序

```powershell
# 构建应用程序
dotnet build AutoScheduling3.csproj -c Debug -r win-x64

# 或者在 Visual Studio 中按 F5 运行
```

### 2. 预期的路径格式

应用程序应该使用以下路径结构：

**LocalFolder 基础路径:**
```
C:\Users\[用户名]\AppData\Local\Packages\[PackageFamilyName]\LocalState\
```

**数据库文件:**
```
[LocalFolder]\GuardDutyScheduling.db
```

**配置文件:**
```
[LocalFolder]\Settings\config.json
```

**备份文件夹:**
```
[LocalFolder]\backups\
```

## 测试步骤

### 步骤 1: 启动应用程序

1. 运行应用程序
2. 观察控制台输出（Debug 窗口）
3. 查找以下日志消息：

```
[DatabaseConfiguration] LocalFolder path: ...
[DatabaseConfiguration] Database path: ...
StoragePathService initialized - LocalFolder: ...
StoragePathService - Database path: ...
StoragePathService - Configuration path: ...
```

**验证点:**
- ✅ 所有路径都包含 `\Packages\` 和 `\LocalState\`
- ✅ 数据库路径直接在 LocalState 根目录
- ✅ 配置文件路径在 LocalState\Settings 子文件夹

### 步骤 2: 导航到设置页面

1. 在应用程序主窗口中，找到并点击"设置"菜单项
2. 等待设置页面加载
3. 滚动到"存储文件路径"部分

**验证点:**
- ✅ 设置页面成功加载
- ✅ 可以看到"存储文件路径"卡片
- ✅ 显示文件列表（至少包含数据库和配置文件）

### 步骤 3: 检查显示的路径

在"存储文件路径"部分，应该看到以下文件：

#### 数据库文件
- **名称:** 数据库文件
- **描述:** 存储排班数据的SQLite数据库
- **路径:** 应该包含 `\LocalState\GuardDutyScheduling.db`
- **状态:** 存在（绿色标签）或不存在（灰色标签）

#### 配置文件
- **名称:** 配置文件
- **描述:** 应用程序设置和用户偏好
- **路径:** 应该包含 `\LocalState\Settings\config.json`
- **状态:** 存在（绿色标签）或不存在（灰色标签）

**验证点:**
- ✅ 路径中包含 `\Packages\[PackageFamilyName]\LocalState\`
- ✅ 路径中**不包含** `AutoScheduling3` 子文件夹
- ✅ 数据库文件在 LocalState 根目录
- ✅ 配置文件在 LocalState\Settings 子文件夹
- ✅ 显示文件大小和最后修改时间

### 步骤 4: 测试"复制路径"功能

#### 测试数据库文件路径复制

1. 找到数据库文件卡片
2. 点击"复制路径"按钮
3. 应该看到成功提示："已复制到剪贴板"
4. 打开记事本（Notepad）
5. 粘贴（Ctrl+V）

**验证点:**
- ✅ 显示成功提示消息
- ✅ 粘贴的路径与界面显示的路径完全一致
- ✅ 路径格式正确，可以在文件资源管理器中使用

#### 测试配置文件路径复制

1. 找到配置文件卡片
2. 点击"复制路径"按钮
3. 粘贴到记事本验证

**验证点:**
- ✅ 成功复制配置文件路径
- ✅ 路径包含 `\Settings\config.json`

### 步骤 5: 测试"打开目录"功能

#### 测试打开数据库文件目录

1. 找到数据库文件卡片
2. 点击"打开目录"按钮
3. 应该打开 Windows 文件资源管理器

**验证点:**
- ✅ 文件资源管理器成功打开
- ✅ 导航到正确的目录（LocalState 文件夹）
- ✅ 数据库文件被选中（高亮显示）
- ✅ 可以在地址栏看到完整路径

**检查目录内容:**
- ✅ `GuardDutyScheduling.db` 文件存在
- ✅ `Settings` 文件夹存在
- ✅ `backups` 文件夹存在（如果已创建过备份）

#### 测试打开配置文件目录

1. 找到配置文件卡片
2. 点击"打开目录"按钮
3. 应该打开 Settings 子文件夹

**验证点:**
- ✅ 打开 Settings 文件夹
- ✅ `config.json` 文件被选中
- ✅ 地址栏显示 `\LocalState\Settings`

### 步骤 6: 测试刷新功能

1. 点击"刷新信息"按钮
2. 观察加载指示器（应该短暂显示）
3. 文件信息应该重新加载

**验证点:**
- ✅ 显示加载动画
- ✅ 文件信息成功刷新
- ✅ 文件大小和最后修改时间更新（如果有变化）

### 步骤 7: 验证路径一致性

比较以下位置的路径是否完全一致：

1. **设置界面显示的路径**
2. **复制到剪贴板的路径**
3. **文件资源管理器中的实际路径**
4. **Debug 输出窗口中的日志路径**

**验证点:**
- ✅ 所有路径完全一致
- ✅ 所有路径都使用 ApplicationData.Current.LocalFolder
- ✅ 路径中不包含旧的 "AutoScheduling3" 子文件夹

## 额外验证

### 验证日志输出

在 Visual Studio 的输出窗口（Output Window）中查找：

```
[DatabaseConfiguration] LocalFolder path: C:\Users\...\LocalState
[DatabaseConfiguration] Database path: C:\Users\...\LocalState\GuardDutyScheduling.db
StoragePathService initialized - LocalFolder: C:\Users\...\LocalState
StoragePathService - Database path: C:\Users\...\LocalState\GuardDutyScheduling.db
StoragePathService - Configuration path: C:\Users\...\LocalState\Settings\config.json
```

### 验证文件实际位置

使用文件资源管理器手动导航到：

1. 按 `Win + R` 打开运行对话框
2. 输入: `%LocalAppData%\Packages`
3. 找到以 `AutoScheduling3` 开头的文件夹
4. 进入 `LocalState` 文件夹
5. 验证文件结构：

```
LocalState\
├── GuardDutyScheduling.db
├── Settings\
│   └── config.json
└── backups\
    └── (备份文件，如果有)
```

## 常见问题排查

### 问题 1: 路径显示为空或错误

**可能原因:**
- StoragePathService 未正确初始化
- ApplicationData.Current.LocalFolder 访问失败

**解决方法:**
1. 检查 Debug 输出中的错误消息
2. 验证应用程序有正确的权限
3. 重新构建并运行应用程序

### 问题 2: "复制路径"功能不工作

**可能原因:**
- 剪贴板访问权限问题
- ClipboardHelper 实现错误

**解决方法:**
1. 检查应用程序清单文件中的权限设置
2. 查看 Debug 输出中的异常信息

### 问题 3: "打开目录"功能失败

**可能原因:**
- 文件或目录不存在
- ProcessHelper 实现问题
- 路径格式错误

**解决方法:**
1. 验证文件确实存在
2. 检查路径格式是否正确
3. 查看 Debug 输出中的错误消息

### 问题 4: 路径包含旧的 "AutoScheduling3" 文件夹

**可能原因:**
- 代码未正确更新
- 使用了缓存的旧路径

**解决方法:**
1. 清理并重新构建项目
2. 检查 DatabaseConfiguration 和 ConfigurationService 的实现
3. 确认使用 ApplicationData.Current.LocalFolder.Path

## 测试完成检查清单

- [ ] 应用程序成功启动
- [ ] 设置页面正常加载
- [ ] 显示存储文件路径部分
- [ ] 数据库文件路径正确（包含 LocalState）
- [ ] 配置文件路径正确（包含 LocalState\Settings）
- [ ] 路径中不包含 "AutoScheduling3" 子文件夹
- [ ] "复制路径"功能正常工作
- [ ] "打开目录"功能正常工作
- [ ] 刷新功能正常工作
- [ ] 所有路径一致（界面、剪贴板、文件系统、日志）
- [ ] Debug 输出显示正确的路径
- [ ] 文件实际存在于正确的位置

## 需求覆盖

本测试覆盖以下需求：

- **Requirement 7.1**: 设置界面显示当前使用的 ApplicationData.Current.LocalFolder 路径 ✅
- **Requirement 7.2**: 设置界面显示数据库文件的完整路径 ✅
- **Requirement 7.3**: 设置界面显示备份文件夹的路径 ✅
- **Requirement 7.4**: 设置界面提供一个按钮来打开数据存储文件夹 ✅

## 测试结果记录

**测试日期:** _______________

**测试人员:** _______________

**测试结果:** ⬜ 通过 / ⬜ 失败

**发现的问题:**
```
1. _______________________________________________
2. _______________________________________________
3. _______________________________________________
```

**备注:**
```
_______________________________________________
_______________________________________________
_______________________________________________
```
