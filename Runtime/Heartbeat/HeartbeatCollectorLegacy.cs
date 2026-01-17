#if (UNITY_EDITOR)

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Compilation;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// LEGACY: Original HeartbeatCollector with performance issues.
    /// Kept for reference. Use HeartbeatCollector (optimized) instead.
    /// </summary>
    [Obsolete("Use HeartbeatCollector instead. This legacy version has performance issues.")]
    public class HeartbeatCollectorLegacy : IHeartbeatCollector
    {
        public event EventHandler<HeartbeatData> OnHeartbeat;

        private AuraSyncSettings Settings { get; set; }
        private IGitClient GitClient { get; }
        private IAuraSyncLogger Logger { get; }

        private float _lastTickTime = 0f;
        private const float PERIODIC_CHECK_INTERVAL = 60f;
        private Dictionary<string, DateTime> _lastModifiedAssets = new Dictionary<string, DateTime>();

        private EditorWindow _lastActiveWindow;

        public HeartbeatCollectorLegacy(AuraSyncSettings settings, IAuraSyncLogger logger = null)
        {
            Settings = settings;
            Logger = logger;

            GitClient = new GitClient(Logger); // Assumindo GitClient é a implementação concreta

            // Registrar callbacks para os eventos do Editor básico
            EditorApplication.playModeStateChanged += EditorApplication_PlayModeStateChanged;
            EditorApplication.contextualPropertyMenu += EditorApplication_ContextualPropertyMenu;
            EditorApplication.hierarchyChanged += EditorApplication_HierarchyChanged;
            EditorSceneManager.sceneSaved += EditorSceneManager_SceneSaved;
            EditorSceneManager.sceneOpened += EditorSceneManager_SceneOpened;
            EditorSceneManager.sceneClosing += EditorSceneManager_SceneClosing;
            EditorSceneManager.newSceneCreated += EditorSceneManager_NewSceneCreated;

            // Eventos adicionais para maior cobertura
            EditorApplication.update += EditorApplication_Update;
            EditorApplication.projectChanged += EditorApplication_ProjectChanged;
            Selection.selectionChanged += Selection_SelectionChanged;

            // Eventos de importação de assets
            AssetDatabase.importPackageStarted += AssetDatabase_ImportPackageStarted;
            AssetDatabase.importPackageCompleted += AssetDatabase_ImportPackageCompleted;
            AssetDatabase.importPackageCancelled += AssetDatabase_ImportPackageCancelled;
            AssetDatabase.importPackageFailed += AssetDatabase_ImportPackageFailed;

            // Eventos de compilação
            CompilationPipeline.compilationStarted += CompilationPipeline_CompilationStarted;
            CompilationPipeline.compilationFinished += CompilationPipeline_CompilationFinished;
            
            // Eventos de Propriedade (Inspector)
            // Não há um evento nativo para "mudança de propriedade" no Inspector em tempo real.
            // Precisamos monitorar a seleção e o EditorApplication.update para o Inspector,
            // ou usar SerializedObject.ApplyModifiedProperties para capturar saves.
            // Para a menor versão, Selection.selectionChanged é um bom proxy.

            // Inicializar o monitoramento de modificação de assets
            InitializeAssetModificationMonitor();

            // Emitir heartbeat de inicialização
            EmitHeartbeat(CreateHeartbeat("Editor Session Start", HeartbeatCategories.EditorSession));
        }

        public void Dispose()
        {
            // Remover callbacks ao descartar o coletor
            EditorApplication.playModeStateChanged -= EditorApplication_PlayModeStateChanged;
            EditorApplication.contextualPropertyMenu -= EditorApplication_ContextualPropertyMenu;
            EditorApplication.hierarchyChanged -= EditorApplication_HierarchyChanged;
            EditorSceneManager.sceneSaved -= EditorSceneManager_SceneSaved;
            EditorSceneManager.sceneOpened -= EditorSceneManager_SceneOpened;
            EditorSceneManager.sceneClosing -= EditorSceneManager_SceneClosing;
            EditorSceneManager.newSceneCreated -= EditorSceneManager_NewSceneCreated;

            // Remover callbacks para os novos eventos
            EditorApplication.update -= EditorApplication_Update;
            EditorApplication.projectChanged -= EditorApplication_ProjectChanged;
            Selection.selectionChanged -= Selection_SelectionChanged;

            AssetDatabase.importPackageStarted -= AssetDatabase_ImportPackageStarted;
            AssetDatabase.importPackageCompleted -= AssetDatabase_ImportPackageCompleted;
            AssetDatabase.importPackageCancelled -= AssetDatabase_ImportPackageCancelled;
            AssetDatabase.importPackageFailed -= AssetDatabase_ImportPackageFailed;

            CompilationPipeline.compilationStarted -= CompilationPipeline_CompilationStarted;
            CompilationPipeline.compilationFinished -= CompilationPipeline_CompilationFinished;

            // Emitir heartbeat de encerramento de sessão
            EmitHeartbeat(CreateHeartbeat("Editor Session End", HeartbeatCategories.EditorSession));
        }

        private void EmitHeartbeat(Heartbeat heartbeat)
        {
            try
            {
                if (heartbeat == null)
                {
                    Logger?.LogWarning("Attempted to emit a null heartbeat");
                    return;
                }
                
                // O logger já é injetado, use-o
                Logger?.Log($"Heartbeat: {heartbeat.Entity}, Category: {heartbeat.Category.GetDescription()}");

                try
                {
                    // Converter para HeartbeatData e emitir o evento
                    HeartbeatData heartbeatData = HeartbeatData.FromHeartbeat(heartbeat);
                    OnHeartbeat?.Invoke(this, heartbeatData);
                }
                catch (System.Exception ex)
                {
                    Logger?.LogWarning($"Failed to emit heartbeat event: {ex.Message}");
                }
            }
            catch (System.Exception ex)
            {
                // Capturar qualquer exceção para evitar que erros no sistema de heartbeat
                // afetem a experiência do usuário com o Unity Editor
                Logger?.LogWarning($"Error in EmitHeartbeat: {ex.Message}");
            }
        }

        /// <summary>
        /// Tenta determinar a entidade primária de um evento.
        /// </summary>
        private string GetEntity(UnityEngine.SceneManagement.Scene? scene = null, UnityEngine.Object obj = null)
        {
            string entityPath = "Unknown"; // Default

            // Prioriza o objeto selecionado se houver
            if (obj != null)
            {
                entityPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(entityPath))
                {
                    // Se não for um asset (ex: GameObject na cena)
                    entityPath = $"GameObject: {obj.name}";
                    if (obj is GameObject go)
                    {
                        string fullPath = go.name;
                        Transform current = go.transform.parent;
                        while (current != null)
                        {
                            fullPath = $"{current.name}/{fullPath}";
                            current = current.parent;
                        }
                        entityPath = $"GameObject Path: {fullPath}";
                    }
                }
            }
            // Em seguida, a cena ativa
            else if (scene != null && !string.IsNullOrEmpty(scene.Value.path))
            {
                entityPath = scene.Value.path;
            }
            // Como fallback, a cena ativa do editor
            else
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.path))
                {
                    entityPath = activeScene.path;
                }
                else
                {
                    entityPath = Application.dataPath; // Representa o projeto
                }
            }
            
            // Converte para caminho absoluto para consistência
            if (entityPath.StartsWith("Assets/"))
            {
                return Application.dataPath + entityPath.Substring("Assets".Length);
            }
            else if (entityPath.StartsWith("GameObject:"))
            {
                // Deixa como está para GameObjects virtuais
            }
            else if (entityPath == Application.dataPath)
            {
                 // Representa o root do projeto
            }
            else if (Path.IsPathRooted(entityPath))
            {
                // Já é um caminho absoluto
            }
            else if (!string.IsNullOrEmpty(entityPath))
            {
                // Tentar um caminho relativo padrão se for apenas um nome de arquivo/pacote
                return Path.Combine(Application.dataPath, entityPath);
            }

            return entityPath;
        }

        /// <summary>
        /// Cria um objeto Heartbeat preenchendo o máximo de informações possível.
        /// </summary>
        private Heartbeat CreateHeartbeat(string entity, HeartbeatCategories? category = null, bool isWrite = false, string eventDetails = null)
        {

            Heartbeat heartbeat = new Heartbeat
            {
                Entity = entity,
                Timestamp = DateTimeExtensions.ToIso8601String(DateTime.UtcNow),
                IsWrite = isWrite,
                Category = category ?? (Application.isPlaying ? HeartbeatCategories.Debugging : HeartbeatCategories.Coding),
                EntityType = EntityTypes.Other, // Definir um default, será refinado abaixo
                UnityVersion = Application.unityVersion,
                OSPlatform = SystemInfo.operatingSystem,
                EventDetails = eventDetails
            };

            // Tentar determinar EntityType e EntityRelativePath e EntityFileType
            if (!string.IsNullOrEmpty(entity) && !entity.StartsWith("GameObject:"))
            {
                string assetPath = entity;
                if (entity.StartsWith(Application.dataPath)) // Converte de absoluto para Assets/
                {
                    assetPath = "Assets" + entity.Substring(Application.dataPath.Length);
                }
                
                heartbeat.EntityRelativePath = assetPath;
                heartbeat.EntityFileType = Path.GetExtension(assetPath)?.ToLowerInvariant().TrimStart('.');
                
                // string guid = AssetDatabase.AssetPathToGUID(assetPath);
                // if (!string.IsNullOrEmpty(guid))
                // {
                //     heartbeat.EntityGuid = guid;
                // }

                // Refinar EntityType com base na extensão ou caminho
                if (assetPath.EndsWith(".unity")) heartbeat.EntityType = EntityTypes.Scene;
                else if (assetPath.EndsWith(".prefab")) heartbeat.EntityType = EntityTypes.Prefab;
                else if (heartbeat.EntityFileType == "cs") heartbeat.EntityType = EntityTypes.File; // Script
                else if (assetPath.Contains("ScriptableObjects") || heartbeat.EntityFileType == "asset") heartbeat.EntityType = EntityTypes.ScriptableObject; // Pode ser um ScriptableObject
                else if (assetPath.Contains("Assets/")) heartbeat.EntityType = EntityTypes.Asset; // Um asset genérico
                else if (Path.GetFileName(assetPath).Contains(".")) // Se tiver extensão, é um arquivo/asset
                {
                    // Já definido acima.
                }
                else if (Directory.Exists(entity)) // Se a entidade for uma pasta
                {
                    heartbeat.EntityType = EntityTypes.Folder;
                }
            }
            else if (entity.StartsWith("GameObject:")) // Para GameObjects virtuais
            {
                heartbeat.EntityType = EntityTypes.GameObject;
                // Extrair o caminho completo do GameObject se estiver presente
                if (entity.StartsWith("GameObject Path: "))
                {
                    heartbeat.SelectedGameObjectPath = entity.Substring("GameObject Path: ".Length);
                }
            }

            // Informações da cena ativa
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                heartbeat.SceneName = activeScene.name;
            }

            // Tentar obter a janela ativa do editor
            if (EditorWindow.focusedWindow != null)
            {
                heartbeat.ActiveEditorWindow = EditorWindow.focusedWindow.GetType().Name;
            }
            
            // Preencher branch name
            if (GitClient != null)
            {
                heartbeat.BranchName = GitClient.GetBranchName(Application.dataPath);
            }

            // TaskId e TaskName serão preenchidos via integração externa (ClickUp) ou manual
            // heartbeat.TaskId = Settings.CurrentTaskId; // Exemplo de como viria das configurações
            // heartbeat.TaskName = Settings.CurrentTaskName; // Exemplo

            return heartbeat;
        }

        #region Editor Event Handlers (Refatorados)

        private void EditorApplication_ContextualPropertyMenu(GenericMenu menu, SerializedProperty property)
        {
            var entity = GetEntity(obj: property.serializedObject.targetObject);
            // Tentar obter o nome da propriedade para mais contexto
            var propertyName = property.displayName;
            var heartbeat = CreateHeartbeat(entity, HeartbeatCategories.InspectorEditing, eventDetails: $"Property Context: {propertyName}");
            heartbeat.SelectedPropertyName = propertyName; // Preenche o novo campo
            EmitHeartbeat(heartbeat);
        }

        private void EditorSceneManager_NewSceneCreated(UnityEngine.SceneManagement.Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            var entity = GetEntity(scene);
            var heartbeat = CreateHeartbeat(entity, HeartbeatCategories.SceneEditing, eventDetails: $"New Scene Created: {scene.name}");
            heartbeat.IsWrite = true; // Criar nova cena é uma escrita
            EmitHeartbeat(heartbeat);
        }

        private void EditorSceneManager_SceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
        {
            var entity = GetEntity(scene);
            var heartbeat = CreateHeartbeat(entity, HeartbeatCategories.SceneEditing, eventDetails: $"Scene Closing: {scene.name}");
            EmitHeartbeat(heartbeat);
        }

        private void EditorSceneManager_SceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            var entity = GetEntity(scene);
            var heartbeat = CreateHeartbeat(entity, HeartbeatCategories.SceneEditing, eventDetails: $"Scene Opened: {scene.name}");
            EmitHeartbeat(heartbeat);
        }

        private void EditorSceneManager_SceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            var entity = GetEntity(scene);
            var heartbeat = CreateHeartbeat(entity, HeartbeatCategories.SceneEditing, isWrite: true, eventDetails: $"Scene Saved: {scene.name}");
            EmitHeartbeat(heartbeat);
        }

        private void EditorApplication_HierarchyChanged()
        {
            // Captura seleção de GameObject
            if (Selection.activeGameObject != null)
            {
                var entity = GetEntity(obj: Selection.activeGameObject);
                var heartbeat = CreateHeartbeat(entity, HeartbeatCategories.SceneEditing, eventDetails: $"Hierarchy Changed: {Selection.activeGameObject.name}");
                EmitHeartbeat(heartbeat);
            }
            else // Se a hierarquia mudou mas nenhum GameObject está ativo (ex: deletou um objeto)
            {
                var entity = GetEntity(); // A cena ativa
                var heartbeat = CreateHeartbeat(entity, HeartbeatCategories.SceneEditing, eventDetails: "Hierarchy Changed (No Active GameObject)");
                EmitHeartbeat(heartbeat);
            }
        }

        /// <summary>
        /// Executa uma ação de forma segura com tratamento de erros para evitar que exceções impactem o usuário
        /// </summary>
        private void SafeExecute(Action action, string operationName = "operation")
        {
            try
            {
                action?.Invoke();
            }
            catch (System.Exception ex)
            {
                // Log silencioso para não incomodar o usuário com erros no sistema de telemetria
                Logger?.LogWarning($"Error during {operationName}: {ex.Message}");
            }
        }
        
        private void EditorApplication_PlayModeStateChanged(PlayModeStateChange obj)
        {
            SafeExecute(() => 
            {
                var entity = GetEntity();
                HeartbeatCategories category = HeartbeatCategories.Coding; // Default

                string eventDetails = $"Play Mode State Changed: {obj}";

                switch (obj)
                {
                    case PlayModeStateChange.EnteredPlayMode:
                        category = HeartbeatCategories.Debugging;
                        break;
                    case PlayModeStateChange.ExitingPlayMode:
                        category = HeartbeatCategories.Debugging; // Saindo do modo de debug
                        break;
                    case PlayModeStateChange.EnteredEditMode:
                        category = HeartbeatCategories.Coding; // Retornando à edição
                        break;
                    case PlayModeStateChange.ExitingEditMode:
                        // Sem categoria específica, pode ser preparação para play mode
                        break;
                }

                var heartbeat = CreateHeartbeat(entity, category, eventDetails: eventDetails);
                EmitHeartbeat(heartbeat);
            }, "PlayModeStateChanged event handling");
        }

        #endregion

        #region Novos e Refatorados Eventos do Editor

        private void EditorApplication_Update()
        {
            SafeExecute(() => 
            {
                // Checagem periódica
                if (Time.realtimeSinceStartup - _lastTickTime > PERIODIC_CHECK_INTERVAL)
                {
                    _lastTickTime = Time.realtimeSinceStartup;
                    var entity = Application.dataPath; // Representa o projeto em geral para o tick
                    var heartbeat = CreateHeartbeat(entity, HeartbeatCategories.EditorSession, eventDetails: "Periodic Editor Check");
                    EmitHeartbeat(heartbeat);

                    // Verificar modificações de arquivos em Assets que o Unity não capturaria
                    CheckForAssetModifications();
                }

                // Monitorar mudança de janela ativa
                if (EditorWindow.focusedWindow != _lastActiveWindow)
                {
                    _lastActiveWindow = EditorWindow.focusedWindow;
                    if (_lastActiveWindow != null)
                    {
                        var entity = GetEntity(); // Entidade base
                        var heartbeat = CreateHeartbeat(entity, HeartbeatCategories.EditorSession, eventDetails: $"Active Window: {_lastActiveWindow.GetType().Name}");
                        EmitHeartbeat(heartbeat);
                    }
                }
            }, "EditorUpdate event handling");
        }
        
        private void InitializeAssetModificationMonitor()
        {
            _lastModifiedAssets.Clear();
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (string path in allAssetPaths)
            {
                if (File.Exists(path) && !path.StartsWith("Packages/") && !path.StartsWith("Library/")) // Evita assets de pacotes/externos
                {
                    _lastModifiedAssets[path] = File.GetLastWriteTime(path);
                }
            }
        }

        private void CheckForAssetModifications()
        {
            SafeExecute(() => 
            {
                // Verifica os assets mais importantes (scripts, cenas, prefabs, etc.)
                // Esta checagem complementa os eventos nativos do AssetDatabase.
                // É mais útil para mudanças externas ou salvamentos não capturados pelos eventos AssetDatabase.
                
                List<string> modifiedPaths = new List<string>();
                try
                {
                    foreach (var entry in _lastModifiedAssets.ToList()) // ToList para poder modificar o dicionário
                    {
                        try 
                        {
                            string path = entry.Key;
                            DateTime lastKnownModTime = entry.Value;

                            if (File.Exists(path))
                            {
                                DateTime currentModTime = File.GetLastWriteTime(path);
                                if (currentModTime > lastKnownModTime)
                                {
                                    modifiedPaths.Add(path);
                                    _lastModifiedAssets[path] = currentModTime; // Atualiza o timestamp conhecido
                                }
                            }
                            else // Arquivo foi deletado
                            {
                                modifiedPaths.Add(path); // Ou registrar um evento de delete
                                _lastModifiedAssets.Remove(path);
                            }
                        }
                        catch (System.IO.IOException ioEx)
                        {
                            // Tratar erros de IO de forma silenciosa para não afetar o usuário
                            Logger?.LogWarning($"IO exception while checking file modifications: {ioEx.Message}");
                        }
                        catch (System.UnauthorizedAccessException authEx)
                        {
                            // Tratar erros de acesso não autorizado
                            Logger?.LogWarning($"Access denied while checking file modifications: {authEx.Message}");
                        }
                        catch (System.Exception ex)
                        {
                            Logger?.LogWarning($"Error checking file modification: {ex.Message}");
                        }
                    }

                    foreach (string path in modifiedPaths)
                    {
                        var heartbeat = CreateHeartbeat(path, HeartbeatCategories.AssetManagement, isWrite: true, eventDetails: "File System Change Detected");
                        EmitHeartbeat(heartbeat);
                    }
                }
                catch (System.Exception ex)
                {
                    Logger?.LogWarning($"Error in checking asset modifications: {ex.Message}");
                }
            }, "Asset modification check");
        }

        private void EditorApplication_ProjectChanged()
        {
            SafeExecute(() => 
            {
                var entity = Application.dataPath; // Representa uma mudança geral no projeto (e.g. foco, reimport)
                var heartbeat = CreateHeartbeat(entity, HeartbeatCategories.AssetManagement, eventDetails: "Project Folder Changed");
                EmitHeartbeat(heartbeat);
            }, "ProjectChanged event handling");
        }

        private void Selection_SelectionChanged()
        {
            SafeExecute(() => 
            {
                if (Selection.activeObject != null)
                {
                    var entity = GetEntity(obj: Selection.activeObject);
                    HeartbeatCategories category = HeartbeatCategories.ProjectBrowse; // Default para seleção
                    string eventDetails = $"Selected: {Selection.activeObject.name}";

                    if (Selection.activeObject is GameObject)
                    {
                        category = HeartbeatCategories.SceneEditing; // Selecionando GameObject na cena
                    }
                    else if (Selection.activeObject is UnityEditor.DefaultAsset && AssetDatabase.IsMainAsset(Selection.activeObject))
                    {
                        // Pode ser uma pasta ou um asset que não é script/cena/prefab mas tem um .asset file
                        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                        if (Directory.Exists(assetPath))
                        {
                            category = HeartbeatCategories.ProjectBrowse; // Navegando em pasta
                            eventDetails = $"Selected Folder: {Selection.activeObject.name}";
                        }
                        else
                        {
                            category = HeartbeatCategories.AssetManagement; // Seleção de asset genérico
                        }
                    }

                    var heartbeat = CreateHeartbeat(entity, category, eventDetails: eventDetails);
                    EmitHeartbeat(heartbeat);
                }
                // Se nada estiver selecionado, não emita um heartbeat específico aqui para evitar spam
            }, "SelectionChanged event handling");
        }

        private void AssetDatabase_ImportPackageStarted(string packageName)
        {
            SafeExecute(() => 
            {
                var heartbeat = CreateHeartbeat($"Package: {packageName}", HeartbeatCategories.AssetManagement, eventDetails: $"Import Package Started: {packageName}");
                heartbeat.EntityType = EntityTypes.Package;
                EmitHeartbeat(heartbeat);
            }, "ImportPackageStarted event handling");
        }

        private void AssetDatabase_ImportPackageCompleted(string packageName)
        {
            var heartbeat = CreateHeartbeat($"Package: {packageName}", HeartbeatCategories.AssetManagement, eventDetails: $"Package Import Completed: {packageName}");
            heartbeat.EntityType = EntityTypes.Package;
            EmitHeartbeat(heartbeat);
        }

        private void AssetDatabase_ImportPackageCancelled(string packageName)
        {
            var heartbeat = CreateHeartbeat($"Package: {packageName}", HeartbeatCategories.AssetManagement, eventDetails: $"Package Import Cancelled: {packageName}");
            heartbeat.EntityType = EntityTypes.Package;
            EmitHeartbeat(heartbeat);
        }

        private void AssetDatabase_ImportPackageFailed(string packageName, string errorMessage)
        {
            var heartbeat = CreateHeartbeat($"Package: {packageName}", HeartbeatCategories.AssetManagement, eventDetails: $"Package Import Failed: {packageName}");
            heartbeat.EntityType = EntityTypes.Package;
            EmitHeartbeat(heartbeat);
            Logger?.LogError($"Package import failed: {packageName} - {errorMessage}");
        }

        private void CompilationPipeline_CompilationStarted(object context)
        {
            var entity = Application.dataPath; // O projeto está compilando
            var heartbeat = CreateHeartbeat(entity, HeartbeatCategories.Compiling, eventDetails: "Script Compilation Started");
            EmitHeartbeat(heartbeat);
        }

        private void CompilationPipeline_CompilationFinished(object context)
        {
            var entity = Application.dataPath; // O projeto terminou a compilação
            var heartbeat = CreateHeartbeat(entity, HeartbeatCategories.Compiling, eventDetails: "Script Compilation Finished");
            EmitHeartbeat(heartbeat);
        }

        #endregion
    }
}

#endif