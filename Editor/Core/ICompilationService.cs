using System;
using System.Collections.Generic;

namespace UnityMCPBridge.Core
{
    /// <summary>
    /// Service for tracking compilation errors and warnings.
    /// </summary>
    public interface ICompilationService : IService
    {
        /// <summary>
        /// Event raised when compilation starts.
        /// </summary>
        event Action OnCompilationStarted;

        /// <summary>
        /// Event raised when compilation finishes.
        /// </summary>
        event Action<bool> OnCompilationFinished;

        /// <summary>
        /// Event raised when a new compilation error/warning is received.
        /// </summary>
        event Action<CompilationError> OnErrorReceived;

        /// <summary>
        /// Gets whether Unity is currently compiling.
        /// </summary>
        bool IsCompiling { get; }

        /// <summary>
        /// Gets all current compilation errors.
        /// </summary>
        IReadOnlyList<CompilationError> GetErrors();

        /// <summary>
        /// Gets all current compilation warnings.
        /// </summary>
        IReadOnlyList<CompilationError> GetWarnings();

        /// <summary>
        /// Gets whether the last compilation had errors.
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// Gets whether the last compilation had warnings.
        /// </summary>
        bool HasWarnings { get; }
    }
}
