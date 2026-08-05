using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Painel com todas as ferramentas do projeto, separadas entre o que ainda
/// falta rodar e o que já foi feito (com a data). Existe para o menu Tools não
/// virar uma lista comprida onde não dá para saber o que já está pronto.
/// </summary>
class ProjectToolsWindow : EditorWindow
{
    [MenuItem(ProjectTools.PanelItem, false, 0)]
    static void Open()
    {
        var window = GetWindow<ProjectToolsWindow>("Ferramentas");
        window.minSize = new Vector2(430f, 320f);
        window.Show();
    }

    Vector2 scroll;
    GUIStyle bodyStyle;

    void OnGUI()
    {
        EnsureStyles();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        // Enquanto o projeto só tiver ferramenta de rotina, as duas listas de
        // estado não têm o que mostrar e o painel vira só o lançador delas.
        bool hasTracked = Array.Exists(ProjectTools.All, tool => tool.Tracked);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Ferramentas do projeto", EditorStyles.largeLabel);
        EditorGUILayout.LabelField(
            hasTracked
                ? "As montagens marcam sozinhas quando rodam, e descem para 'Já rodadas'. " +
                  "Desmontar devolve a ferramenta para a lista de cima."
                : "Nenhuma montagem pendente no momento — as que rodaram foram apagadas do " +
                  "projeto, e voltam do git se fizerem falta.",
            bodyStyle);
        EditorGUILayout.Space(10f);

        if (hasTracked)
        {
            DrawSection("A fazer", tool => tool.Tracked && ProjectTools.LastRun(tool.Id) == null,
                        "Nada pendente — todas as montagens já rodaram.");
        }

        DrawSection("Sempre à mão", tool => !tool.Tracked, null);

        if (hasTracked)
        {
            DrawSection("Já rodadas", tool => tool.Tracked && ProjectTools.LastRun(tool.Id) != null,
                        "Ainda não rodou nenhuma montagem nesta máquina.");
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(
            "O que o projeto tem de feito de verdade está no ROADMAP.md. O diário do painel " +
            "fica em UserSettings/, fora do git — ele só responde o que você rodou nesta máquina.",
            bodyStyle);

        EditorGUILayout.EndScrollView();
    }

    void DrawSection(string title, Func<ProjectTools.Tool, bool> filter, string emptyMessage)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        bool any = false;
        foreach (var tool in ProjectTools.All)
        {
            if (!filter(tool))
                continue;

            DrawTool(tool);
            any = true;
        }

        if (!any && emptyMessage != null)
            EditorGUILayout.LabelField(emptyMessage, bodyStyle);

        EditorGUILayout.Space(10f);
    }

    void DrawTool(ProjectTools.Tool tool)
    {
        var lastRun = tool.Tracked ? ProjectTools.LastRun(tool.Id) : null;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(tool.Title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(tool.Summary, bodyStyle);

        if (lastRun.HasValue)
            EditorGUILayout.LabelField("Rodou em " + lastRun.Value.ToString("dd/MM/yyyy 'às' HH:mm"), bodyStyle);

        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(lastRun.HasValue ? "Rodar de novo" : "Rodar", GUILayout.Height(24f)))
            ProjectTools.Run(tool.MenuPath);

        if (!string.IsNullOrEmpty(tool.UndoMenuPath) &&
            GUILayout.Button("Desmontar", GUILayout.Height(24f), GUILayout.Width(110f)))
            ProjectTools.Run(tool.UndoMenuPath);

        // Para quando o registro não bate com a realidade — cena revertida pelo
        // git, por exemplo, que o diário não tem como perceber.
        if (lastRun.HasValue &&
            GUILayout.Button("Esquecer", GUILayout.Height(24f), GUILayout.Width(90f)))
        {
            ProjectTools.Forget(tool.Id);
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4f);
    }

    void EnsureStyles()
    {
        if (bodyStyle != null)
            return;

        bodyStyle = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            richText = false,
        };
    }
}
