# Project Structure

## Architecture Pattern

MVVM (Model-View-ViewModel) with clear separation of concerns and dependency injection.

## Directory Organization

### Core Layers

- **Views/**: XAML views and code-behind (UI layer)
  - `DataManagement/`: Personnel, Position, Skill management pages
  - `Scheduling/`: Scheduling creation, templates, drafts, results
  - `History/`: History list and version comparison
  - `Settings/`: Settings and test data generator

- **ViewModels/**: View models (presentation logic)
  - `Base/`: Base view model classes
  - `DataManagement/`, `Scheduling/`, `History/`, `Settings/`: Corresponding VMs

- **Services/**: Business logic layer
  - `Interfaces/`: Service contracts
  - `ImportExport/`: Data import/export services
  - Core services: `SchedulingService`, `PersonnelService`, `PositionService`, etc.

- **Data/**: Data access layer
  - `Interfaces/`: Repository contracts
  - `Models/`: Database entity models
  - `Enums/`, `Exceptions/`, `Logging/`, `Migrations/`: Supporting components
  - Repository implementations: `PersonalRepository`, `PositionLocationRepository`, etc.

- **SchedulingEngine/**: Scheduling algorithm implementation
  - `Core/`: `FeasibilityTensor`, `SchedulingContext`, `ConstraintValidator`, `ScoreCalculator`
  - `Strategies/`: `MRVStrategy` (Minimum Remaining Values)
  - `GreedyScheduler.cs`: Main scheduler

### Supporting Directories

- **DTOs/**: Data Transfer Objects
  - `Mappers/`: Entity-to-DTO mapping
  - `ImportExport/`: Import/export specific DTOs

- **Models/**: Domain models
  - `Constraints/`: Constraint-related models

- **Helpers/**: Utility classes
  - `NavigationService`, `DialogService`, `AnimationHelper`, etc.

- **Controls/**: Custom UI controls
  - `PersonnelCard`, `PositionCard`, `ScheduleGridControl`, etc.

- **Converters/**: XAML value converters
- **Constants/**: Application constants
- **Extensions/**: Extension methods (including DI configuration)
- **Themes/**: XAML resource dictionaries
- **History/**: History management implementation
- **TestData/**: Test data generation and validation

## Key Files

- **App.xaml.cs**: Application entry point and initialization
- **MainWindow.xaml.cs**: Main application window
- **Extensions/ServiceCollectionExtensions.cs**: Dependency injection configuration
- **Data/DatabaseService.cs**: Database initialization and management
- **Data/DatabaseSchema.cs**: Database schema definitions

## Dependency Injection Order

Services are registered in this order (see `ServiceCollectionExtensions.cs`):

1. **Repositories** (including `DatabaseService` as singleton)
2. **Mappers** (data transformation)
3. **Business Services** (depend on repositories)
4. **Helper Services** (UI and utilities)
5. **ViewModels** (depend on all above)

**Critical**: `DatabaseService` must be initialized via `InitializeAsync()` before other services use it.

## Data Flow

1. **View** → **ViewModel** (user interaction)
2. **ViewModel** → **Service** (business logic)
3. **Service** → **Repository** (data access)
4. **Repository** → **DatabaseService** (database operations)

## Naming Conventions

- **Services**: `I{Name}Service` interface, `{Name}Service` implementation
- **Repositories**: `I{Name}Repository` interface, `{Name}Repository` implementation
- **ViewModels**: `{Name}ViewModel`
- **DTOs**: `{Name}Dto`
- **Async methods**: Always use `Async` suffix
- **Private fields**: Use `_camelCase` prefix
- **Observable properties**: Use `[ObservableProperty]` attribute with `_camelCase` backing field
