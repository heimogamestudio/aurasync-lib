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
                
                // Carregar configurações predefinidas (não modificáveis pelo usuário)
                _settings = AuraSyncSettings.CreateDefault();
                
                // Inicializar logger
                _logger = new DefaultLogger();
                _logger.Log("AuraSync initializing...");
                
                try
                {
                    // Inicializar coletor de heartbeats com as configurações predefinidas
                    _heartbeatCollector = new HeartbeatCollector(_settings, _logger);
                    
                    // Inicializar sender de heartbeats
                    _heartbeatSender = new HeartbeatSender(_settings, _logger);
                    
                    // Registrar callback para lidar com eventos de heartbeat
                    (_heartbeatCollector as HeartbeatCollector).OnHeartbeat += OnHeartbeatReceived;
                    
                    _logger.Log("Heartbeat tracking initialized");
                }
                catch (System.Exception ex)
                {
                    // Falha na inicialização dos serviços, mas não impede o Unity de funcionar
                    _logger.LogWarning($"Failed to initialize AuraSync services: {ex.Message}");
                }
                
                _initialized = true;
                _logger.Log("AuraSync initialized successfully!");
            }
            catch (System.Exception ex)
            {
                // Falha na inicialização geral
                Debug.LogWarning($"[AuraSync] Initialization error: {ex.Message}");
            }
#else
            Debug.Log("AuraSync initialized successfully!");
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
                // Enviar o heartbeat para o backend
                _heartbeatSender?.SendHeartbeat(heartbeatData);
            }
            catch (System.Exception ex)
            {
                // Não permitir que erros no envio de heartbeats afetem a experiência do usuário
                if (_logger != null)
                {
                    _logger.LogWarning($"Failed to send heartbeat: {ex.Message}");
                }
            }
        }
#endif
    }
}
