#if (UNITY_EDITOR)

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Compilation;
using UnityEngine;

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// OPTIMIZED HeartbeatCollector with performance improvements:
    /// - Debounce on frequent events (2-second cooldown)
    /// - No more polling all assets (removed InitializeAssetModificationMonitor)
    /// - Reduced EditorApplication.update overhead
    /// - Event tags for frontend categorization
    /// </summary>
    public class HeartbeatCollector : IHeartbeatCollector
    {
        public event EventHandler<HeartbeatData> OnHeartbeat;

        private AuraSyncSettings Settings { get; set; }
        private IGitClient GitClient { get; }
        private IAuraSyncLogger Logger { get; }

        // === Performance: Timing ===
        private const float PERIODIC_CHECK_INTERVAL = 120f; // Increased from 60s to 120s
        private const float DEBOUNCE_INTERVAL = 2f;         // 2 seconds between same-type events
        private const float INACTIVITY_THRESHOLD = 300f;    // 5 minutes - stop pinging if no activity
        private double _nextPeriodicCheck = 0;
        private double _lastRealActivity = 0;              // Track last non-ping activity
        private EditorWindow _lastActiveWindow;
        
        // === Performance: Debounce tracking ===
        private Dictionary<EventTag, double> _lastEventTime = new Dictionary<EventTag, double>();
        
        // === Session tracking ===
        private bool _sessionStarted = false;
        private string _cachedBranchName;
        private double _lastBranchCheck = 0;
        private const float BRANCH_CHECK_INTERVAL = 300f; // Check git branch every 5 minutes

        public HeartbeatCollector(AuraSyncSettings settings, IAuraSyncLogger logger = null)
        {
            Settings = settings;
            Logger = logger;
            GitClient = new GitClient(Logger);
            
            // Cache initial branch
            _cachedBranchName = GitClient?.GetBranchName(Application.dataPath) ?? "unknown";
            
            // Initialize activity tracking
            _lastRealActivity = EditorApplication.timeSinceStartup;

            // === Register Editor Callbacks ===
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.projectChanged += OnProjectChanged;
            
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorSceneManager.newSceneCreated += OnNewSceneCreated;
            
            Selection.selectionChanged += OnSelectionChanged;
            
            // Asset imports
            AssetDatabase.importPackageCompleted += OnPackageImportCompleted;
            AssetDatabase.importPackageFailed += OnPackageImportFailed;
            
            // Compilation
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            
            // Emit session start
            EmitSessionStart();
        }

        public void Dispose()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.projectChanged -= OnProjectChanged;
            
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
            EditorSceneManager.newSceneCreated -= OnNewSceneCreated;
            
            Selection.selectionChanged -= OnSelectionChanged;
            
            AssetDatabase.importPackageCompleted -= OnPackageImportCompleted;
            AssetDatabase.importPackageFailed -= OnPackageImportFailed;
            
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            
            // Emit session end
            EmitHeartbeat(CreateHeartbeat("Session End", EventTag.SessionEnd));
        }

        #region Performance: Debounce & Throttle

        /// <summary>
        /// Check if enough time has passed since last event of this type.
        /// Returns true if event should be emitted, false to skip.
        /// </summary>
        private bool ShouldEmitEvent(EventTag tag, float customDebounce = -1)
        {
            double now = EditorApplication.timeSinceStartup;
            float debounce = customDebounce > 0 ? customDebounce : DEBOUNCE_INTERVAL;
            
            if (_lastEventTime.TryGetValue(tag, out double lastTime))
            {
                if (now - lastTime < debounce)
                {
                    return false; // Too soon, skip
                }
            }
            
            _lastEventTime[tag] = now;
            return true;
        }

        /// <summary>
        /// OPTIMIZED: Only runs actual logic when needed, not every frame
        /// </summary>
        private void OnEditorUpdate()
        { - ONLY if there was recent activity
            if (now >= _nextPeriodicCheck)
            {
                _nextPeriodicCheck = now + PERIODIC_CHECK_INTERVAL;
                
                // Check if developer has been active in the last INACTIVITY_THRESHOLD seconds
                double timeSinceLastActivity = now - _lastRealActivity;
                bool isActive = timeSinceLastActivity < INACTIVITY_THRESHOLD;
                
                if (isActive)
                {
                    // Developer is active, send ping
                    EmitHeartbeat(CreateHeartbeat(Application.productName, EventTag.SessionPing));
                }
                // else: Developer is idle/away, don't send unnecessary pings
                
                // Also refresh git branch periodically (only if active)
                if (isActive && Heartbeat(CreateHeartbeat(Application.productName, EventTag.SessionPing));
                
                // Also refresh git branch periodically
                if (_lastRealActivity = now; // Update activity timestamp
                    now - _lastBranchCheck > BRANCH_CHECK_INTERVAL)
                {
                    _lastBranchCheck = now;
                    _cachedBranchName = GitClient?.GetBranchName(Application.dataPath) ?? _cachedBranchName;
                }
            }
            
            // Window focus change (with debounce)
            if (EditorWindow.focusedWindow != _lastActiveWindow)
            {
                _lastActiveWindow = EditorWindow.focusedWindow;
                if (_lastActiveWindow != null && ShouldEmitEvent(EventTag.WindowFocus))
                {
                    EmitHeartbeat(CreateHeartbeat(
                        GetCurrentEntity(), 
                        EventTag.WindowFocus,
                        details: _lastActiveWindow.GetType().Name
                    ));
                }
            }
        }

        #endregion

        #region Event Handlers

        private void EmitSessionStart()
        {
            if (_sessionStarted) return;
            _sessionStarted = true;
            
            var heartbeat = CreateHeartbeat(Application.productName, EventTag.SessionStart);
            heartbeat.UnityVersion = Application.unityVersion;
            heartbeat.OSPlatform = SystemInfo.operatingSystem;
            EmitHeartbeat(heartbeat);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            SafeExecute(() =>
            {
                EventTag tag = state switch
                {
                    PlayModeStateChange.EnteredPlayMode => EventTag.PlayStart,
                    PlayModeStateChange.ExitingPlayMode => EventTag.PlayStop,
                    _ => EventTag.Other
                };
                
                if (tag != EventTag.Other)
                {
                    EmitHeartbeat(CreateHeartbeat(GetCurrentEntity(), tag, details: state.ToString()));
                }
            });
        }

        private void OnHierarchyChanged()
        {
            SafeExecute(() =>
            {
                // Debounce: hierarchy changes can be very frequent
                if (!ShouldEmitEvent(EventTag.HierarchyChange)) return;
                
                string entity = Selection.activeGameObject != null 
                    ? $"GameObject: {Selection.activeGameObject.name}" 
                    : GetCurrentEntity();
                    
                EmitHeartbeat(CreateHeartbeat(entity, EventTag.HierarchyChange, isWrite: true));
            });
        }

        private void OnProjectChanged()
        {
            SafeExecute(() =>
            {
                if (!ShouldEmitEvent(EventTag.AssetModify, 5f)) return; // 5 second debounce
                EmitHeartbeat(CreateHeartbeat(Application.productName, EventTag.AssetModify, isWrite: true));
            });
        }

        private void OnSelectionChanged()
        {
            SafeExecute(() =>
            {
                if (Selection.activeObject == null) return;
                if (!ShouldEmitEvent(EventTag.SelectionChange)) return;
                
                string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                EventTag tag = EventTag.SelectionChange;
                
                // Determine more specific tag based on selection
                if (!string.IsNullOrEmpty(assetPath))
                {
                    if (assetPath.EndsWith(".cs"))
                    {
                        tag = EventTag.CodeEdit;
                    }
                    else if (assetPath.EndsWith(".unity"))
                    {
                        tag = EventTag.SceneOpen;
                    }
                }
                else if (Selection.activeGameObject != null)
                {
                    tag = EventTag.HierarchyChange;
                }
                
                EmitHeartbeat(CreateHeartbeat(
                    assetPath ?? $"GameObject: {Selection.activeObject.name}",
                    tag,
                    details: Selection.activeObject.name
                ));
            });
        }

        private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            SafeExecute(() =>
            {
                EmitHeartbeat(CreateHeartbeat(scene.path, EventTag.SceneSave, isWrite: true, details: scene.name));
            });
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            SafeExecute(() =>
            {
                EmitHeartbeat(CreateHeartbeat(scene.path, EventTag.SceneOpen, details: scene.name));
            });
        }

        private void OnSceneClosing(UnityEngine.SceneManagement.Scene scene, bool removing)
        {
            SafeExecute(() =>
            {
                EmitHeartbeat(CreateHeartbeat(scene.path, EventTag.SceneClose, details: scene.name));
            });
        }

        private void OnNewSceneCreated(UnityEngine.SceneManagement.Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            SafeExecute(() =>
            {
                EmitHeartbeat(CreateHeartbeat(scene.path, EventTag.SceneCreate, isWrite: true, details: scene.name));
            });
        }

        private void OnPackageImportCompleted(string packageName)
        {
            SafeExecute(() =>
            {
                var heartbeat = CreateHeartbeat($"Package: {packageName}", EventTag.PackageImport, details: packageName);
                heartbeat.EntityType = EntityTypes.Package;
                EmitHeartbeat(heartbeat);
            });
        }

        private void OnPackageImportFailed(string packageName, string error)
        {
            SafeExecute(() =>
            {
                var heartbeat = CreateHeartbeat($"Package: {packageName}", EventTag.PackageFailed, details: error);
                heartbeat.EntityType = EntityTypes.Package;
                EmitHeartbeat(heartbeat);
            });
        }

        private void OnCompilationStarted(object context)
        {
            SafeExecute(() =>
            {
                EmitHeartbeat(CreateHeartbeat(Application.productName, EventTag.CompileStart, details: "Compilation Started"));
            });
        }

        private void OnCompilationFinished(object context)
        {
            SafeExecute(() =>
            {
                EmitHeartbeat(CreateHeartbeat(Application.productName, EventTag.CompileEnd, details: "Compilation Finished"));
            });
        }

        #endregion

        #region Heartbeat Creation

        private Heartbeat CreateHeartbeat(string entity, EventTag eventTag, bool isWrite = false, string details = null)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            string relativePath = GetRelativePath(entity);
            
            return new Heartbeat
            {
                Entity = entity,
                EntityRelativePath = relativePath,
                EntityFileType = GetFileExtension(entity),
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                IsWrite = isWrite,
                BranchName = _cachedBranchName,
                EventTag = eventTag,
                Category = GetCategoryFromTag(eventTag),
                EntityType = GetEntityType(entity),
                SceneName = activeScene.IsValid() ? activeScene.name : null,
                ActiveEditorWindow = EditorWindow.focusedWindow?.GetType().Name,
                EventDetails = details
            };
        }

        private void EmitHeartbeat(Heartbeat heartbeat)
        {
            if (heartbeat == null) return;
            
            try
            {
                Logger?.Log($"[{heartbeat.EventTag}] {heartbeat.EventDetails ?? heartbeat.Entity}");
                OnHeartbeat?.Invoke(this, HeartbeatData.FromHeartbeat(heartbeat));
            }
            catch (Exception ex)
            {
                Logger?.LogWarning($"Error emitting heartbeat: {ex.Message}");
            }
        }

        private void SafeExecute(Action action)
        {
            try { action?.Invoke(); }
            catch (Exception ex) { Logger?.LogWarning($"HeartbeatCollector error: {ex.Message}"); }
        }

        #endregion

        #region Utilities

        private string GetCurrentEntity()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            return activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.path) 
                ? activeScene.path 
                : Application.productName;
        }

        private string GetRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            
            int assetsIndex = path.IndexOf("Assets/", StringComparison.Ordinal);
            if (assetsIndex >= 0) return path.Substring(assetsIndex);
            
            return path;
        }

        private string GetFileExtension(string path)
        {
            if (string.IsNullOrEmpty(path) || path.StartsWith("GameObject:")) return null;
            return Path.GetExtension(path)?.TrimStart('.').ToLowerInvariant();
        }

        private EntityTypes GetEntityType(string entity)
        {
            if (string.IsNullOrEmpty(entity)) return EntityTypes.Other;
            if (entity.StartsWith("GameObject:")) return EntityTypes.GameObject;
            if (entity.StartsWith("Package:")) return EntityTypes.Package;
            if (entity.EndsWith(".unity")) return EntityTypes.Scene;
            if (entity.EndsWith(".prefab")) return EntityTypes.Prefab;
            if (entity.EndsWith(".cs")) return EntityTypes.File;
            if (entity.EndsWith(".asset")) return EntityTypes.ScriptableObject;
            return EntityTypes.Asset;
        }

        private HeartbeatCategories GetCategoryFromTag(EventTag tag)
        {
            return tag switch
            {
                EventTag.CodeEdit or EventTag.CodeSave => HeartbeatCategories.Coding,
                EventTag.CompileStart or EventTag.CompileEnd => HeartbeatCategories.Compiling,
                EventTag.SceneOpen or EventTag.SceneSave or EventTag.SceneCreate or EventTag.SceneClose => HeartbeatCategories.SceneEditing,
                EventTag.HierarchyChange => HeartbeatCategories.SceneEditing,
                EventTag.PlayStart or EventTag.PlayStop => HeartbeatCategories.Debugging,
                EventTag.AssetImport or EventTag.AssetModify or EventTag.PackageImport or EventTag.PackageFailed => HeartbeatCategories.AssetManagement,
                EventTag.InspectorEdit => HeartbeatCategories.InspectorEditing,
                EventTag.ProjectBrowse or EventTag.SelectionChange => HeartbeatCategories.ProjectBrowse,
                EventTag.SessionStart or EventTag.SessionEnd or EventTag.SessionPing or EventTag.WindowFocus => HeartbeatCategories.EditorSession,
                _ => HeartbeatCategories.Other
            };
        }

        #endregion
    }
}

#endif
