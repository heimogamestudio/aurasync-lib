#if (UNITY_EDITOR)

using System;
using UnityEngine;
using System.IO;

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// Internal representation of a developer activity event in Unity Editor.
    /// </summary>
    public class Heartbeat
    {
        // === Core Fields ===
        public string Entity { get; set; }
        public string Timestamp { get; set; }
        public bool IsWrite { get; set; }
        public string BranchName { get; set; }
        public HeartbeatCategories Category { get; set; }
        public EntityTypes EntityType { get; set; }
        
        // === NEW: Event Tag for Frontend Display ===
        public EventTag EventTag { get; set; } = EventTag.Other;

        // === Context Fields ===
        public string EntityRelativePath { get; set; }
        public string EntityFileType { get; set; }
        public string SceneName { get; set; }
        public string ActiveEditorWindow { get; set; }
        public string SelectedGameObjectPath { get; set; }
        public string SelectedPropertyName { get; set; }
        public string UnityVersion { get; set; }
        public string OSPlatform { get; set; }
        public string EventDetails { get; set; }
    }
}

#endif