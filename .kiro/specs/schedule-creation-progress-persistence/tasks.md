# Implementation Plan

- [x] 1. Create draft data transfer objects





  - Create `SchedulingDraftDto` class with all required properties for serializing schedule creation state
  - Create `ManualAssignmentDraftDto` class for serializing temporary manual assignments
  - Include version field for compatibility checking
  - Add JSON serialization attributes for proper serialization
  - _Requirements: 1.5, 2.1, 2.2, 4.2_


- [x] 2. Implement draft service interface and implementation





  - [x] 2.1 Create `ISchedulingDraftService` interface

    - Define methods: `SaveDraftAsync`, `LoadDraftAsync`, `DeleteDraftAsync`, `HasDraftAsync`, `CleanupExpiredDraftsAsync`
    - _Requirements: 1.1, 1.2, 1.3, 3.1, 3.3_
  - [x] 2.2 Implement `SchedulingDraftService` class


    - Use `ApplicationData.Current.LocalSettings` for storage
    - Implement JSON serialization/deserialization for draft data
    - Add draft expiration logic (7 days)
    - Include error handling for corrupted data
    - _Requirements: 1.2, 1.3, 3.3, 5.3_


- [ ] 3. Add draft management to SchedulingViewModel

  - [x] 3.1 Create `SchedulingViewModel.Draft.cs` partial class


    - Inject `ISchedulingDraftService` dependency
    - Implement `CreateDraftAsync()` method to collect current state
    - Implement `RestoreFromDraftAsync()` method to restore state
    - Implement `ValidateDraftDataAsync()` for data validation
    - _Requirements: 1.1, 1.4, 5.1, 5.2, 5.5_
  - [x] 3.2 Implement draft restoration helper methods


    - Create `RestoreSelectedPersonnelAndPositionsAsync()` to restore selections
    - Create `RestoreConstraintsAsync()` to restore constraint configurations
    - Create `RestoreManualAssignmentsAsync()` to restore manual assignments
    - Handle missing personnel/positions with validation
    - _Requirements: 1.4, 4.3, 4.4, 5.5_
  - [x] 3.3 Add draft validation logic


    - Validate version compatibility
    - Validate date ranges (adjust if StartDate is in the past)
    - Validate personnel and position IDs exist in available lists
    - Validate template references
    - Display appropriate warnings for validation failures
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 4. Integrate draft persistence into CreateSchedulingPage





  - [x] 4.1 Modify `OnNavigatedTo` lifecycle method


    - Check for existing draft using `HasDraftAsync()`
    - Show ContentDialog asking user to restore or start fresh
    - Call `RestoreFromDraftAsync()` if user chooses to restore
    - Delete draft if user chooses to start fresh
    - Display temporary notification at page top: "已从草稿恢复，上次编辑时间：XXX" (auto-dismiss after 3 seconds)
    - _Requirements: 1.3, 1.4, 3.2_
  - [x] 4.2 Modify `OnNavigatedFrom` lifecycle method


    - Check if there's progress worth saving using `ShouldSaveDraft()`
    - Call `CreateDraftAsync()` to collect current state
    - Call `SaveDraftAsync()` to persist draft
    - Display brief notification: "当前进度已保存为草稿" (optional, non-blocking)
    - _Requirements: 1.1, 1.2_
  - [x] 4.3 Implement `ShouldSaveDraft()` helper method


    - Check if ScheduleTitle is not empty
    - Check if any personnel or positions are selected
    - Check if any manual assignments exist
    - Return false if ResultSchedule exists (successful creation)
    - _Requirements: 1.1, 3.1_
-

- [x] 5. Add draft cleanup on successful schedule creation



  - [x] 5.1 Modify `ExecuteSchedulingAsync` in SchedulingViewModel


    - Call `DeleteDraftAsync()` after successful schedule creation
    - Ensure draft is deleted before navigation to result page
    - _Requirements: 3.1_

- [-] 6. Add draft handling to cancel operation


  - [ ] 6.1 Modify `CancelWizard` method in SchedulingViewModel
    - Check if there's progress worth saving
    - Show ContentDialog asking user to keep or discard draft with message "当前进度将保存为草稿"
    - Call `DeleteDraftAsync()` if user chooses to discard
    - Allow draft to be saved automatically in `OnNavigatedFrom` if user chooses to keep
    - _Requirements: 3.2_

- [ ] 7. Register draft service in dependency injection

  - [ ] 7.1 Update `ServiceCollectionExtensions.cs`
    - Register `ISchedulingDraftService` as singleton
    - Ensure service is available for injection into ViewModel and Page
    - _Requirements: 1.1, 1.2, 1.3_

- [ ] 8. Handle template mode draft restoration

  - [ ] 8.1 Add template mode detection in draft
    - Save `TemplateApplied` and `LoadedTemplateId` in draft
    - Restore template mode state when loading draft
    - Handle missing template scenario (switch to manual mode)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 5.1_

- [ ] 9. Implement draft expiration cleanup

  - [ ] 9.1 Add cleanup logic to application startup
    - Call `CleanupExpiredDraftsAsync()` during app initialization
    - Remove drafts older than 7 days
    - _Requirements: 3.3_

- [ ] 10. Add error handling and user notifications

  - [ ] 10.1 Handle draft load failures
    - Catch JSON deserialization errors
    - Display warning using DialogService
    - Delete corrupted draft and show empty form
    - _Requirements: 5.3, 5.4_
  - [ ] 10.2 Handle data inconsistency scenarios
    - Show warnings for missing personnel/positions
    - Show warnings for missing templates
    - Show warnings for invalid manual assignments
    - Adjust dates automatically if needed
    - _Requirements: 5.1, 5.2, 5.4, 5.5_
