using UnityEditor;
using UnityEngine;

/// <summary>
/// Ferramenta de teste do salvamento no Editor. No celular o salvamento é
/// outro arquivo, e se apaga desinstalando o app ou limpando os dados dele.
/// </summary>
static class SaveTools
{
    [MenuItem(ProjectTools.DeleteSaveItem, false, 161)]
    static void DeleteSave()
    {
        if (!EditorUtility.DisplayDialog("Apagar o salvamento",
                                         "Apaga níveis das naves, moeda, progresso das fases e recordes " +
                                         "deste computador. Não tem volta.",
                                         "Apagar", "Cancelar"))
            return;

        SaveGame.Delete();
        Debug.Log($"[Salvamento] Apagado: {SaveGame.FilePath}. A próxima partida começa do zero.");
    }
}
