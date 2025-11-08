using System;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.Data.Enums;
using AutoScheduling3.Data.Models;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// Manages the state and progress of database initialization
    /// Provides thread-safe state tracking and prevents concurrent initialization
    /// Requirements: 1.5, 9.1, 9.2, 9.3, 9.5
    /// </summary>
    public class InitializationStateManager
    {
        private InitializationState _currentState;
        private readonly SemaphoreSlim _stateLock;
        private InitializationProgress _progress;

        /// <summary>
        /// Initializes a new instance of the InitializationStateManager
        /// </summary>
        public InitializationStateManager()
        {
            _currentState = InitializationState.NotStarted;
            _stateLock = new SemaphoreSlim(1, 1);
            _progress = new InitializationProgress
            {
                StartedAt = DateTime.UtcNow,
                CurrentStage = InitializationStage.DirectoryCreation,
                CurrentMessage = "Initialization not started"
            };
        }

        /// <summary>
        /// Attempts to begin initialization, preventing concurrent initialization attempts
        /// Requirements: 1.5, 9.1, 9.2
        /// </summary>
        /// <returns>True if initialization can begin, false if already in progress</returns>
        public async Task<bool> TryBeginInitializationAsync()
        {
            await _stateLock.WaitAsync();
            try
            {
                if (_currentState == InitializationState.InProgress)
                {
                    return false;
                }

                _currentState = InitializationState.InProgress;
                _progress = new InitializationProgress
                {
                    StartedAt = DateTime.UtcNow,
                    CurrentStage = InitializationStage.DirectoryCreation,
                    CurrentMessage = "Starting initialization"
                };

                return true;
            }
            finally
            {
                _stateLock.Release();
            }
        }

        /// <summary>
        /// Updates the final state after initialization completes or fails
        /// Requirements: 9.1, 9.3
        /// </summary>
        /// <param name="success">Whether initialization succeeded</param>
        public void CompleteInitialization(bool success)
        {
            _stateLock.Wait();
            try
            {
                _currentState = success ? InitializationState.Completed : InitializationState.Failed;
                
                if (_progress != null)
                {
                    _progress.CompletedAt = DateTime.UtcNow;
                    _progress.CurrentMessage = success 
                        ? "Initialization completed successfully" 
                        : "Initialization failed";
                }
            }
            finally
            {
                _stateLock.Release();
            }
        }

        /// <summary>
        /// Gets the current initialization state
        /// Requirements: 9.3
        /// </summary>
        /// <returns>The current state</returns>
        public InitializationState GetCurrentState()
        {
            _stateLock.Wait();
            try
            {
                return _currentState;
            }
            finally
            {
                _stateLock.Release();
            }
        }

        /// <summary>
        /// Updates the progress with the current stage and message
        /// Requirements: 9.5
        /// </summary>
        /// <param name="stage">The current initialization stage</param>
        /// <param name="message">A descriptive message about the current operation</param>
        public void UpdateProgress(InitializationStage stage, string message)
        {
            _stateLock.Wait();
            try
            {
                if (_progress != null)
                {
                    // Add the previous stage to completed stages if it's different
                    if (_progress.CurrentStage != stage && 
                        !_progress.CompletedStages.Contains(_progress.CurrentStage.ToString()))
                    {
                        _progress.CompletedStages.Add(_progress.CurrentStage.ToString());
                    }

                    _progress.CurrentStage = stage;
                    _progress.CurrentMessage = message ?? string.Empty;
                }
            }
            finally
            {
                _stateLock.Release();
            }
        }

        /// <summary>
        /// Gets the current initialization progress information
        /// Requirements: 9.5
        /// </summary>
        /// <returns>A copy of the current progress</returns>
        public InitializationProgress GetProgress()
        {
            _stateLock.Wait();
            try
            {
                if (_progress == null)
                {
                    return new InitializationProgress
                    {
                        StartedAt = DateTime.UtcNow,
                        CurrentStage = InitializationStage.DirectoryCreation,
                        CurrentMessage = "No initialization in progress"
                    };
                }

                // Return a copy to prevent external modification
                return new InitializationProgress
                {
                    CurrentStage = _progress.CurrentStage,
                    CurrentMessage = _progress.CurrentMessage,
                    StartedAt = _progress.StartedAt,
                    CompletedAt = _progress.CompletedAt,
                    CompletedStages = new System.Collections.Generic.List<string>(_progress.CompletedStages)
                };
            }
            finally
            {
                _stateLock.Release();
            }
        }
    }
}
