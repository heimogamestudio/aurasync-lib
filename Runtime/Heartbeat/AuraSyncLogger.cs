#if (UNITY_EDITOR)

using System;

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// Interface para loggers do AuraSync
    /// </summary>
    public interface IAuraSyncLogger
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
    }
    
    /// <summary>
    /// Implementação padrão de Logger
    /// </summary>
    public class DefaultLogger : IAuraSyncLogger
    {
        public void Log(string message)
        {
 #if AURA_SYNC_DEBUG           
            UnityEngine.Debug.Log($"[AuraSync] {message}");
#endif
        }

        public void LogWarning(string message)
        {
#if AURA_SYNC_DEBUG
            UnityEngine.Debug.LogWarning($"[AuraSync] {message}");
#endif
        }

        public void LogError(string message)
        {
#if AURA_SYNC_DEBUG
            UnityEngine.Debug.LogError($"[AuraSync] {message}");
#endif
        }
    }
}

#endif
