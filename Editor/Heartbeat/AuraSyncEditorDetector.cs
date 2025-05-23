#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using Heimo.AuraSync.Heartbeat;

namespace Heimo.AuraSync.Editor
{
    /// <summary>
    /// Detecta eventos de início e término do Unity Editor
    /// </summary>
    [InitializeOnLoad]
    public static class AuraSyncEditorDetector
    {
        // Este construtor estático é chamado quando o Unity Editor inicia
        static AuraSyncEditorDetector()
        {
            // Adicionar delay para permitir que o Editor inicialize completamente
            EditorApplication.delayCall += DetectEditorStart;
            
            // Assinar o evento de desligamento do editor
            EditorApplication.quitting += DetectEditorQuit;
            
            // Verificar por modificações de arquivos externos ao Unity
            AssetDatabase.Refresh();
        }
        
        private static void DetectEditorStart()
        {
            try
            {
                var logger = new DefaultLogger();
                logger.Log("[AuraSync] Editor inicializado - detectando atividade do desenvolvedor");

                // Inicializar o AuraSync no início do Editor - agora usando versão estática
                AuraSyncManager.Initialize();
            }
            catch (System.Exception ex)
            {
                // Não mostra erro ao usuário final, apenas registra internamente
                Debug.LogWarning($"[AuraSync] Falha ao inicializar: {ex.Message}");
            }
        }
        
        private static void DetectEditorQuit()
        {
            try
            {
                var logger = new DefaultLogger();
                logger.Log("[AuraSync] Editor está sendo fechado - registrando evento final");

                // Desligar o AuraSync ao fechar o editor (isso vai disparar o evento final)
                AuraSyncManager.Shutdown();
            }
            catch (System.Exception ex)
            {
                // Não mostra erro ao usuário final, apenas registra internamente
                Debug.LogWarning($"[AuraSync] Falha ao desligar: {ex.Message}");
            }
        }
    }
}

#endif
