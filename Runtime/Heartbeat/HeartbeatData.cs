#if (UNITY_EDITOR)

using System;
using UnityEngine; // Necessário para [Serializable]

namespace Heimo.AuraSync.Heartbeat // Mantendo o namespace atual, ajuste se o pacote for Pulse
{
    /// <summary>
    /// Dados detalhados do heartbeat para serialização e envio ao backend.
    /// Esta é a estrutura do objeto 'heartbeat_data' dentro do payload.
    /// </summary>
    [Serializable]
    public class HeartbeatData
    {
        public string entity; // Caminho completo do arquivo/entidade ou descrição do evento
        public string timestamp; // Unix timestamp float
        public bool is_write; // True se a ação foi uma escrita/modificação
        public string branch_name; // Nome do branch Git
        public string category; // Categoria principal da atividade (string do enum)
        public string entity_type; // Tipo da entidade envolvida (string do enum)

        // --- Novos Campos para Informações Mais Úteis (Correspondem ao Heartbeat) ---

        public string entity_relative_path; // Caminho da entidade relativo à pasta Assets/
        public string entity_file_type; // Extensão do arquivo (ex: "cs", "unity", "prefab")

        public string scene_name; // Nome da cena ativa no momento do heartbeat
        public string active_editor_window; // Nome da janela ativa do Editor (ex: "SceneView", "InspectorWindow")

        public string selected_game_object_path; // Caminho completo do GameObject selecionado na hierarquia
        public string selected_property_name; // Nome da propriedade modificada no Inspector

        public string unity_version; // Versão completa do Unity Editor
        public string os_platform; // Plataforma do S.O. (ex: "Windows", "macOS")
        public string event_details; // Ex: "Compilation started", "Package 'DOTween' imported"
        public string time_zone_offset; // Offset do fuso horário local

        /// <summary>
        /// Converte um objeto Heartbeat interno para a estrutura serializável HeartbeatData.
        /// </summary>
        public static HeartbeatData FromHeartbeat(Heartbeat heartbeat)
        {
            if (heartbeat == null) return null;

            return new HeartbeatData
            {
                entity = heartbeat.Entity,
                timestamp = heartbeat.Timestamp,
                is_write = heartbeat.IsWrite,
                branch_name = heartbeat.BranchName,
                category = heartbeat.Category.GetDescription(), // Usa o método de extensão
                entity_type = heartbeat.EntityType.GetDescription(), // Usa o método de extensão

                // --- Mapeando os Novos Campos ---
                entity_relative_path = heartbeat.EntityRelativePath,
                entity_file_type = heartbeat.EntityFileType,
                scene_name = heartbeat.SceneName,
                active_editor_window = heartbeat.ActiveEditorWindow,
                selected_game_object_path = heartbeat.SelectedGameObjectPath,
                selected_property_name = heartbeat.SelectedPropertyName,
                unity_version = heartbeat.UnityVersion,
                os_platform = heartbeat.OSPlatform,
                event_details = heartbeat.EventDetails,
                time_zone_offset = heartbeat.TimeZoneOffset
            };
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