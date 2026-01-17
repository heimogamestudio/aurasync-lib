using System;
using System.Net.NetworkInformation;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// Configurações do AuraSync
    /// Classe com configurações predefinidas que não podem ser alteradas pelo usuário
    /// </summary>
    [Serializable]
    public class AuraSyncSettings
    {
        // Propriedades somente leitura para evitar modificação externa
        public string User { get; private set; } = "";
        public string ProjectName { get; private set; } = "";
        public string BackendUrl { get; private set; } = "https://ulgebuochosphlsmfmrz.supabase.co/functions/v1/log";
        public string ApiKey { get; private set; } = "aurasync_test_key_1234567890";
        public bool EnableHeartbeats { get; private set; } = true;
        
        /// <summary>
        /// Cria uma nova instância das configurações com valores padrão detectados automaticamente
        /// </summary>
        public static AuraSyncSettings CreateDefault()
        {
            var settings = new AuraSyncSettings
            {
                User = UnityEditor.CloudProjectSettings.userName,
                ProjectName = Application.productName,
                EnableHeartbeats = true
            };
            
            return settings;
        }
        
        /// <summary>
        /// Retorna as configurações predefinidas
        /// </summary>
        /// <returns>Uma nova instância de AuraSyncSettings com os valores automáticos detectados</returns>
        public static AuraSyncSettings Load()
        {
            // Sempre retorna uma instância nova com os valores atualizados (nome do usuário, project, etc)
            return CreateDefault();
        }
    }
}
#endif
