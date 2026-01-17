#if (UNITY_EDITOR)

using System.ComponentModel;

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// Event tags for frontend display - human readable labels with icons
    /// These are used in the dashboard to categorize and filter events
    /// </summary>
    public enum EventTag
    {
        // === Coding & Scripts ===
        [Description("code_edit")]
        CodeEdit,           // 💻 Editing scripts
        
        [Description("code_save")]
        CodeSave,           // 💾 Saving scripts
        
        [Description("compile_start")]
        CompileStart,       // ⚙️ Compilation started
        
        [Description("compile_end")]
        CompileEnd,         // ✅ Compilation finished
        
        // === Scene & Hierarchy ===
        [Description("scene_open")]
        SceneOpen,          // 📂 Scene opened
        
        [Description("scene_save")]
        SceneSave,          // 💾 Scene saved
        
        [Description("scene_create")]
        SceneCreate,        // ✨ New scene created
        
        [Description("scene_close")]
        SceneClose,         // 📁 Scene closed
        
        [Description("hierarchy_change")]
        HierarchyChange,    // 🔀 GameObject added/removed/moved
        
        // === Assets & Imports ===
        [Description("asset_import")]
        AssetImport,        // 📦 Asset imported
        
        [Description("asset_modify")]
        AssetModify,        // ✏️ Asset modified
        
        [Description("package_import")]
        PackageImport,      // 📦 Package imported
        
        [Description("package_failed")]
        PackageFailed,      // ❌ Package import failed
        
        // === Testing & Debug ===
        [Description("play_start")]
        PlayStart,          // ▶️ Entered play mode
        
        [Description("play_stop")]
        PlayStop,           // ⏹️ Exited play mode
        
        // === Editor Activity ===
        [Description("window_focus")]
        WindowFocus,        // 🪟 Window focus changed
        
        [Description("selection_change")]
        SelectionChange,    // 👆 Selection changed
        
        [Description("inspector_edit")]
        InspectorEdit,      // 🔧 Property edited in inspector
        
        [Description("project_browse")]
        ProjectBrowse,      // 📁 Browsing project files
        
        // === Session ===
        [Description("session_start")]
        SessionStart,       // 🚀 Editor session started
        
        [Description("session_end")]
        SessionEnd,         // 👋 Editor session ended
        
        [Description("session_ping")]
        SessionPing,        // 💓 Periodic heartbeat
        
        // === Other ===
        [Description("other")]
        Other               // 📝 Other activity
    }
    
    /// <summary>
    /// Utility class for event tag metadata used by frontend
    /// </summary>
    public static class EventTagMetadata
    {
        /// <summary>
        /// Get display info for frontend rendering
        /// </summary>
        public static (string Label, string Icon, string Color) GetDisplayInfo(EventTag tag)
        {
            return tag switch
            {
                // Coding
                EventTag.CodeEdit => ("Code Edit", "💻", "#3B82F6"),
                EventTag.CodeSave => ("Code Save", "💾", "#10B981"),
                EventTag.CompileStart => ("Compiling", "⚙️", "#F59E0B"),
                EventTag.CompileEnd => ("Compiled", "✅", "#10B981"),
                
                // Scene
                EventTag.SceneOpen => ("Scene Open", "📂", "#8B5CF6"),
                EventTag.SceneSave => ("Scene Save", "💾", "#10B981"),
                EventTag.SceneCreate => ("New Scene", "✨", "#EC4899"),
                EventTag.SceneClose => ("Scene Close", "📁", "#6B7280"),
                EventTag.HierarchyChange => ("Hierarchy", "🔀", "#F97316"),
                
                // Assets
                EventTag.AssetImport => ("Import", "📦", "#06B6D4"),
                EventTag.AssetModify => ("Asset Edit", "✏️", "#F59E0B"),
                EventTag.PackageImport => ("Package", "📦", "#8B5CF6"),
                EventTag.PackageFailed => ("Import Failed", "❌", "#EF4444"),
                
                // Testing
                EventTag.PlayStart => ("Play Mode", "▶️", "#10B981"),
                EventTag.PlayStop => ("Stop Play", "⏹️", "#6B7280"),
                
                // Editor
                EventTag.WindowFocus => ("Focus", "🪟", "#6B7280"),
                EventTag.SelectionChange => ("Selection", "👆", "#8B5CF6"),
                EventTag.InspectorEdit => ("Inspector", "🔧", "#F59E0B"),
                EventTag.ProjectBrowse => ("Browse", "📁", "#6B7280"),
                
                // Session
                EventTag.SessionStart => ("Session Start", "🚀", "#10B981"),
                EventTag.SessionEnd => ("Session End", "👋", "#6B7280"),
                EventTag.SessionPing => ("Active", "💓", "#3B82F6"),
                
                _ => ("Activity", "📝", "#6B7280")
            };
        }
    }
}

#endif
