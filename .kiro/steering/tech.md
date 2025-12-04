# Technology Stack

## Framework & Platform

- **.NET 8.0**: Target framework
- **WinUI 3**: UI framework (Windows App SDK 1.8)
- **Windows 10 1809+**: Minimum OS version (Build 17763)
- **Architecture**: MVVM (Model-View-ViewModel)

## Key Libraries

- **CommunityToolkit.Mvvm** (8.4.0): MVVM helpers and source generators
- **CommunityToolkit.WinUI.UI.Controls.DataGrid** (7.1.2): Data grid control
- **Microsoft.Data.Sqlite** (9.0.10): SQLite database access
- **Microsoft.Extensions.DependencyInjection** (9.0.10): Dependency injection
- **MathNet.Numerics** (5.0.0): Mathematical computations
- **Newtonsoft.Json** (13.0.4): JSON serialization
- **EPPlus** (8.2.1): Excel export functionality
- **xUnit** (2.9.3) + **Moq** (4.20.72): Testing frameworks

## Database

- **SQLite**: Local database stored in `ApplicationData.Current.LocalFolder`
- **Default Path**: `%LocalAppData%\Packages\<PackageId>\LocalState\GuardDutyScheduling.db`
- **Backup Location**: `{LocalFolder}\backups\`

## Common Commands

**默认使用 `--verbosity quiet` 只显示错误，保持输出简洁。**

### Build & Run

```bash
# Restore dependencies
dotnet restore

# Build project (默认：只显示错误)
dotnet build --verbosity quiet

# Build release version (默认：只显示错误)
dotnet publish -c Release -r win-x64 --verbosity quiet

# Run application
dotnet run

# Clean build output
dotnet clean

# 如需查看警告，使用 minimal 级别
dotnet build --verbosity minimal
```

### Testing

```bash
# Run all tests (默认：只显示错误)
dotnet test --verbosity quiet

# Run with detailed output
dotnet test --verbosity normal

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura --verbosity quiet
```

### Platform-Specific

```bash
# Build for specific platform (默认：只显示错误)
dotnet build -r win-x64 --verbosity quiet
dotnet build -r win-x86 --verbosity quiet
dotnet build -r win-arm64 --verbosity quiet
```

## Development Tools

- **Visual Studio 2022**: Recommended IDE
- **Windows App SDK**: Required for WinUI 3 development
- **SQLite Browser**: Useful for database inspection

## Library Configuration

### EPPlus

- **License Context**: EPPlus 许可证上下文已在应用程序启动时（`App.xaml.cs`）全局设置为 `NonCommercial`
- **重要**: 不要在其他服务的构造函数中重复设置 `ExcelPackage.LicenseContext`
- **原因**: 全局设置一次即可，重复设置是冗余的且不符合最佳实践
