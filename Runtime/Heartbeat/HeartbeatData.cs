#if (UNITY_EDITOR)

using System;
using UnityEngine;

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// OPTIMIZED heartbeat payload for backend.
    /// Reduced from ~600 bytes to ~200 bytes per event.
    /// Static fields (unity_version, os_platform) sent only once per session.
    /// </summary>
    [Serializable]
    public class HeartbeatData
    {
        // === Core Fields (always sent) ===
        public string entity;           // Relative path only (Assets/Scripts/Player.cs)
        public string timestamp;        // ISO 8601 UTC string
        public bool is_write;           // True if write/modify operation
        public string branch_name;      // Git branch
        
        // === NEW: Event Tag for Frontend Display ===
        public string event_tag;        // Tag code for categorization (e.g., "code_edit", "scene_save")
        
        // === Contextual Fields (sent when relevant) ===
        public string category;         // Activity category (coding, debugging, etc.)
        public string entity_type;      // Type of entity (file, scene, prefab, etc.)
        public string file_ext;         // File extension without dot (cs, unity, prefab)
        public string scene;            // Current scene name (renamed from scene_name)
        public string window;           // Active editor window (renamed from active_editor_window)
        public string details;          // Event-specific details (renamed from event_details)
        
        // === Session-level Fields (sent once per session, not every heartbeat) ===
        // These are now optional and only included in session_start events
        public string unity_ver;        // Unity version (session start only)
        public string os;               // OS platform (session start only)

        /// <summary>
        /// Converts internal Heartbeat to optimized serializable HeartbeatData.
        /// </summary>
        public static HeartbeatData FromHeartbeat(Heartbeat heartbeat)
        {
            if (heartbeat == null) return null;

            var data = new HeartbeatData
            {
                // Core fields - always present
                entity = heartbeat.EntityRelativePath ?? GetRelativePath(heartbeat.Entity),
                timestamp = heartbeat.Timestamp,
                is_write = heartbeat.IsWrite,
                branch_name = heartbeat.BranchName,
                
                // NEW: Event tag for frontend
                event_tag = heartbeat.EventTag.GetDescription(),
                
                // Context fields
                category = heartbeat.Category.GetDescription(),
                entity_type = heartbeat.EntityType.GetDescription(),
                file_ext = heartbeat.EntityFileType,
                scene = heartbeat.SceneName,
                window = heartbeat.ActiveEditorWindow,
                details = heartbeat.EventDetails
            };
            
            // Only include session-level data for session_start events
            if (heartbeat.EventTag == EventTag.SessionStart)
            {
                data.unity_ver = heartbeat.UnityVersion;
                data.os = heartbeat.OSPlatform;
            }
            
            return data;
        }
        
        /// <summary>
        /// Convert absolute path to Assets-relative path
        /// </summary>
        private static string GetRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return "";
            
            int assetsIndex = absolutePath.IndexOf("Assets/", StringComparison.Ordinal);
            if (assetsIndex >= 0)
            {
                return absolutePath.Substring(assetsIndex);
            }
            
            // For non-asset entities (GameObjects, etc.)
            return absolutePath;
        }
    }

    /// <summary>
    /// Payload completo a ser enviado para o backend.
    /// </summary>
    [Serializable]
    public class DevActivityPayload
    {
        public string user;
        public string TaskId;
        public string Project = Application.productName;
        public HeartbeatData heartbeat_data;
        
    }
}

#endif