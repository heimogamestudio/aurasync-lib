#if (UNITY_EDITOR)

using System;
using UnityEngine; // Para Application.platform, Application.unityVersion
using System.IO;   // Para Path.GetExtension

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// Representa um evento de atividade do desenvolvedor no Unity Editor (representação interna).
    /// </summary>
    public class Heartbeat
    {
        public string Entity { get; set; } // Caminho completo do arquivo/entidade ou descrição do evento
        public string Timestamp { get; set; } // Unix timestamp float
        public bool IsWrite { get; set; } // True se a ação foi uma escrita/modificação
        public string BranchName { get; set; } // Nome do branch Git
        public HeartbeatCategories Category { get; set; } // Categoria principal da atividade
        public EntityTypes EntityType { get; set; } // Tipo da entidade envolvida

        // --- Novos Campos para Informações Mais Úteis ---

        public string EntityRelativePath { get; set; } // Caminho da entidade relativo à pasta Assets/
        public string EntityFileType { get; set; } // Extensão do arquivo (ex: "cs", "unity", "prefab")
        public string SceneName { get; set; } // Nome da cena ativa no momento do heartbeat
        public string ActiveEditorWindow { get; set; } // Nome da janela ativa do Editor (ex: "SceneView", "InspectorWindow")

        public string SelectedGameObjectPath { get; set; } // Caminho completo do GameObject selecionado na hierarquia
        public string SelectedPropertyName { get; set; } // Nome da propriedade modificada no Inspector

        public string UnityVersion { get; set; } // Versão completa do Unity Editor
        public string OSPlatform { get; set; } // Plataforma do S.O. (ex: "Windows", "macOS")
        public string EventDetails { get; set; } // Ex: "Compilation started", "Package 'DOTween' imported"
        public string TimeZoneOffset { get; set; } // Offset do fuso horário local
    }
}

#endif