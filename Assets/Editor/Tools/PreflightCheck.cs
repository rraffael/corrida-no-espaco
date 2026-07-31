using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Confere a configuração do projeto antes de gerar build, para os erros
/// baratos aparecerem no Console e não no aparelho — ou, pior, na Play.
/// Não altera nada: só olha e relata.
/// </summary>
static class PreflightCheck
{
    const string OldApplicationId = "com.Raffael.corridanoespaco";

    [MenuItem("Tools/Corrida no Espaço/Conferir configuração", false, 150)]
    static void Check()
    {
        var problems = new List<string>();
        var warnings = new List<string>();
        var notes = new List<string>();

        CheckScenes(problems, notes);
        CheckOrientation(problems, notes);
        CheckSdk(warnings, notes);
        CheckSigning(problems, notes);
        CheckIdentity(warnings, notes);

        Report(problems, warnings, notes);
    }

    static void CheckScenes(List<string> problems, List<string> notes)
    {
        var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();

        if (enabled.Length == 0)
        {
            problems.Add("Nenhuma cena habilitada na build list. O app abriria em tela preta.");
            return;
        }

        notes.Add($"Cenas ({enabled.Length}): {string.Join(" → ", enabled.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)))}");

        // A primeira da lista é a que o app abre. Se a Game vier antes da Menu,
        // o jogo pula o menu sem ninguém entender por quê.
        string first = System.IO.Path.GetFileNameWithoutExtension(enabled[0].path);
        if (first != "Menu")
            problems.Add($"A primeira cena da build list é '{first}', não 'Menu'. É ela que o app abre.");
    }

    static void CheckOrientation(List<string> problems, List<string> notes)
    {
        var orientation = PlayerSettings.defaultInterfaceOrientation;
        notes.Add($"Orientação: {orientation}");

        if (orientation != UIOrientation.Portrait)
        {
            problems.Add(
                $"Orientação está em {orientation}. O jogo foi desenhado em retrato; " +
                "girar a tela no meio da partida quebra o enquadramento.");
        }
    }

    static void CheckSdk(List<string> warnings, List<string> notes)
    {
        int target = (int)PlayerSettings.Android.targetSdkVersion;
        int minimum = (int)PlayerSettings.Android.minSdkVersion;

        notes.Add($"SDK: min {minimum}, target {(target == 0 ? "Automatic" : target.ToString())}");

        if (target == 0)
        {
            warnings.Add(
                "Target SDK está em Automatic. O build passa a depender do que está instalado " +
                "na máquina, e a Play exige um alvo mínimo para aceitar upload.");
        }
    }

    static void CheckSigning(List<string> problems, List<string> notes)
    {
        if (!PlayerSettings.Android.useCustomKeystore)
        {
            notes.Add("Assinatura: keystore de debug (nenhuma chave configurada no projeto)");
            return;
        }

        notes.Add($"Assinatura: keystore próprio — {PlayerSettings.Android.keystoreName}");
        problems.Add(
            "Há um keystore configurado no projeto. Isso escreve o caminho da chave no " +
            "ProjectSettings.asset e pode acabar versionado. O build de release configura e " +
            "limpa a assinatura sozinho: rode Tools → Corrida no Espaço → Build → AAB de release.");
    }

    static void CheckIdentity(List<string> warnings, List<string> notes)
    {
        string id = PlayerSettings.applicationIdentifier;
        notes.Add($"Pacote: {id}  ·  versão {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode})");

        if (id == OldApplicationId)
        {
            warnings.Add(
                $"applicationId ainda é '{OldApplicationId}', o da conta antiga. " +
                "Decidir o definitivo antes do primeiro upload — depois de publicado não muda mais (Fase 3).");
        }
    }

    static void Report(List<string> problems, List<string> warnings, List<string> notes)
    {
        var report = new StringBuilder();
        report.AppendLine("[Preflight] Configuração do projeto");

        foreach (var note in notes)
            report.AppendLine($"   · {note}");

        if (problems.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("Impede um build correto:");
            foreach (var problem in problems)
                report.AppendLine($"   ✗ {problem}");
        }

        if (warnings.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("Resolver antes de publicar:");
            foreach (var warning in warnings)
                report.AppendLine($"   ! {warning}");
        }

        if (problems.Count == 0 && warnings.Count == 0)
            report.AppendLine("\nNada a corrigir.");

        string text = report.ToString();

        if (problems.Count > 0)
            Debug.LogError(text);
        else if (warnings.Count > 0)
            Debug.LogWarning(text);
        else
            Debug.Log(text);
    }
}
