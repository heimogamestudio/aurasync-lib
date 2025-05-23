#if (UNITY_EDITOR)

using System;

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// Interface para coletores de heartbeat que monitoram atividades do desenvolvedor
    /// </summary>
    public interface IHeartbeatCollector : IDisposable
    {
        event EventHandler<HeartbeatData> OnHeartbeat;
    }
}

#endif
