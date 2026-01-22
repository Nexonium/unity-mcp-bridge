using UnityEditor;
using UnityEngine;

namespace UnityMCPBridge.Core
{
    /// <summary>
    /// Thread-safe cache for Unity Editor state.
    /// Updated on main thread, readable from any thread.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorStateCache
    {
        private static volatile bool _isPlaying;
        private static volatile bool _isPaused;
        private static volatile bool _isCompiling;
        private static string _unityVersion;
        private static string _productName;

        /// <summary>
        /// Gets whether Unity is in Play Mode.
        /// </summary>
        public static bool IsPlaying => _isPlaying;

        /// <summary>
        /// Gets whether Unity is paused.
        /// </summary>
        public static bool IsPaused => _isPaused;

        /// <summary>
        /// Gets whether Unity is compiling.
        /// </summary>
        public static bool IsCompiling => _isCompiling;

        /// <summary>
        /// Gets the Unity version string.
        /// </summary>
        public static string UnityVersion => _unityVersion;

        /// <summary>
        /// Gets the project/product name.
        /// </summary>
        public static string ProductName => _productName;

        static EditorStateCache()
        {
            // Initialize static values (these don't change)
            _unityVersion = Application.unityVersion;
            _productName = Application.productName;

            // Initial update
            UpdateState();

            // Subscribe to editor update for continuous state refresh
            EditorApplication.update += UpdateState;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.pauseStateChanged += OnPauseChanged;
        }

        private static void UpdateState()
        {
            _isPlaying = EditorApplication.isPlaying;
            _isPaused = EditorApplication.isPaused;
            _isCompiling = EditorApplication.isCompiling;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            _isPlaying = EditorApplication.isPlaying;
            _isPaused = EditorApplication.isPaused;
        }

        private static void OnPauseChanged(PauseState state)
        {
            _isPaused = state == PauseState.Paused;
        }
    }
}
