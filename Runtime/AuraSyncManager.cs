using UnityEngine;

#if UNITY_EDITOR
using Heimo.AuraSync.Heartbeat;
using UnityEditor;
#endif

namespace Heimo.AuraSync
{
    /// <summary>
    /// Main manager class for AuraSync functionality.
    /// </summary>
    public static class AuraSyncManager
    {
#if UNITY_EDITOR
        private static IHeartbeatCollector _heartbeatCollector;
        private static HeartbeatSender _heartbeatSender;
        private static IAuraSyncLogger _logger;
        private static AuraSyncSettings _settings;
        private static bool _initialized = false;
#endif

        /// <summary>
        /// Initializes the AuraSync system if it hasn't been initialized yet.
        /// </summary>
        public static void EnsureInitialized()
        {
#if UNITY_EDITOR
            if (!_initialized)
            {
                Initialize();
            }
#endif
        }

        /// <summary>
        /// Disposes the AuraSync system resources.
        /// </summary>
        public static void Shutdown()
        {
#if UNITY_EDITOR
            _heartbeatCollector?.Dispose();
            _heartbeatCollector = null;
            _heartbeatSender = null;
            _logger = null;
            _initialized = false;
#endif
        }

        /// <summary>
        /// Initializes the AuraSync system.
        /// </summary>
        public static void Initialize()
        {
#if UNITY_EDITOR
            try
            {
                // Se já inicializado, não faz nada
                if (_initialized)
                    return;

                // Inicializar logger primeiro para poder reportar erros
                _logger = new DefaultLogger();
                _logger.Log("AuraSync initializing...");

                // Carregar configurações predefinidas (não modificáveis pelo usuário)
                _settings = AuraSyncSettings.CreateDefault();
                _logger.Log($"Backend URL: {_settings.BackendUrl}");
                _logger.Log($"User: {_settings.User}");

                // Inicializar coletor de heartbeats com as configurações predefinidas
                _heartbeatCollector = new HeartbeatCollector(_settings, _logger);

                // Inicializar sender de heartbeats
                _heartbeatSender = new HeartbeatSender(_settings, _logger);

                // Registrar callback para lidar com eventos de heartbeat
                (_heartbeatCollector as HeartbeatCollector).OnHeartbeat += OnHeartbeatReceived;

                _initialized = true;
                _logger.Log("AuraSync initialized successfully!");
            }
            catch (System.Exception ex)
            {
                _logger?.LogError($"Initialization error: {ex.Message}");
            }
#endif
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Manipula eventos de heartbeat recebidos do coletor
        /// </summary>
        private static void OnHeartbeatReceived(object sender, HeartbeatData heartbeatData)
        {
            try
            {
                _heartbeatSender?.SendHeartbeat(heartbeatData);
            }
            catch (System.Exception ex)
            {
                _logger?.LogWarning($"Failed to send heartbeat: {ex.Message}");
            }
        }
#endif
    }
}
