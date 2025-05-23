#if (UNITY_EDITOR)

using System.ComponentModel; // Necessário para o atributo [Description]

namespace Heimo.AuraSync.Heartbeat
{
    /// <summary>
    /// Categorias para classificação de atividades do desenvolvedor
    /// </summary>
    public enum HeartbeatCategories
    {
        [Description("coding")]
        Coding,

        [Description("debugging")]
        Debugging,

        [Description("designing")]
        Designing, // Geralmente para trabalho em cenas, UI, modelos

        [Description("building")]
        Building, // Relacionado à construção de builds do jogo

        [Description("testing")]
        Testing, // Usando o Play Mode para testar

        [Description("other")]
        Other,

        [Description("editor_session")]
        EditorSession, // Para heartbeats periódicos ou de início/fim de sessão

        [Description("asset_management")]
        AssetManagement, // Importação, organização, modificação de assets que não são código/cena

        [Description("compiling")]
        Compiling, // Processo de compilação de scripts

        // --- Novas Categorias para mais detalhes ---
        [Description("scene_editing")]
        SceneEditing, // Edição ativa na Scene View ou Hierarchy (movendo objetos, etc.)

        [Description("inspector_editing")]
        InspectorEditing, // Modificando propriedades no Inspector

        [Description("project_Browse")]
        ProjectBrowse, // Navegando na Project View (selecionando pastas/arquivos)

        [Description("version_control")]
        VersionControl // Interações com sistema de controle de versão (fora do Editor, mas relevante para o contexto)
    }

    /// <summary>
    /// Tipos de entidade que podem gerar heartbeats
    /// </summary>
    public enum EntityTypes
    {
        [Description("file")]
        File, // Tipo genérico de arquivo

        [Description("scene")]
        Scene, // Arquivo de cena .unity

        [Description("asset")]
        Asset, // Arquivos .asset, texturas, modelos, etc.

        [Description("prefab")]
        Prefab, // Arquivos .prefab

        [Description("scriptable_object")]
        ScriptableObject, // Arquivos .asset que são ScriptableObjects

        [Description("game_object")]
        GameObject, // Objeto dentro de uma cena ou prefab (não um arquivo)

        [Description("folder")]
        Folder, // Pastas no projeto

        [Description("package")]
        Package, // Pacote Unity (UPM)

        [Description("other")]
        Other
    }
}

#endif