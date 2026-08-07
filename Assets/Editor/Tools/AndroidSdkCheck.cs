using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Diz, no Console, qual SDK do Android o Editor está usando e quais Android SDK
/// Platforms estão instaladas nele.
///
/// **Por que existe:** sem saber o que está no disco, fixar o Target API Level é
/// chute — e foi assim que o build de 31/07/2026 quebrou. Em 07/08/2026 ele
/// mostrou que a `6000.3.20f1` traz 34, 35 e **36**, ou seja, aquele erro tinha
/// outra causa e o medo de mexer no target era infundado.
///
/// Continua valendo para conferir antes de mexer no target, e de novo a cada
/// atualização da Unity ou exigência nova da Play — o conjunto de Platforms muda
/// com a versão do Editor.
/// </summary>
static class AndroidSdkCheck
{
    /// <summary>Nível exigido pela Play a partir de 31/08/2026.</summary>
    const int RequiredApiLevel = 36;

    [MenuItem(ProjectTools.AndroidSdkItem, false, 31)]
    internal static void Run()
    {
        string sdk = ResolveSdkPath();

        if (string.IsNullOrEmpty(sdk) || !Directory.Exists(sdk))
        {
            Debug.LogError(
                "[SDK] Não achei o SDK do Android.\n" +
                "Confira o caminho em Edit → Preferences → External Tools → Android, e se o " +
                "módulo Android está instalado no Unity Hub.");
            return;
        }

        var report = new StringBuilder();
        report.AppendLine("[SDK] Android SDK em uso pelo Editor:");
        report.AppendLine("  " + sdk);
        report.AppendLine();

        string platformsFolder = Path.Combine(sdk, "platforms");
        var levels = InstalledLevels(platformsFolder);

        if (levels.Length == 0)
        {
            report.AppendLine("  Nenhuma Platform instalada em " + platformsFolder);
        }
        else
        {
            report.AppendLine("  Platforms instaladas: " +
                              string.Join(", ", levels.Select(level => "android-" + level)));
        }

        report.AppendLine();
        report.AppendLine("  Target API Level do projeto: " + DescribeTarget());
        report.AppendLine("  Min API Level do projeto: " + (int)PlayerSettings.Android.minSdkVersion);

        bool hasRequired = levels.Contains(RequiredApiLevel);

        report.AppendLine();
        if (hasRequired)
        {
            report.AppendLine($"  ✔ A Platform {RequiredApiLevel} ESTÁ instalada.");
            report.AppendLine($"    Falta só fixar o Target API Level em {RequiredApiLevel} — " +
                              "é ProjectSettings, vai por editor script.");
            Debug.Log(report.ToString());
            return;
        }

        report.AppendLine($"  ✘ A Platform {RequiredApiLevel} NÃO está instalada.");
        report.AppendLine("    Fixar o target nela agora quebra o build.");
        report.AppendLine();
        report.AppendLine("    Para instalar, um destes:");

        string sdkmanager = FindSdkmanager(sdk);
        if (!string.IsNullOrEmpty(sdkmanager))
        {
            report.AppendLine($"    • \"{sdkmanager}\" \"platforms;android-{RequiredApiLevel}\"");
        }

        report.AppendLine("    • SDK Manager do Android Studio, apontando para a pasta acima");

        // Warning e não erro: não ter a 36 hoje não impede nada — o target está
        // em Automatic e o build funciona. Vira erro no dia 31/08.
        Debug.LogWarning(report.ToString());
    }

    /// <summary>
    /// O caminho que o Editor realmente usa. A preferência ganha quando o Raffael
    /// apontou para um SDK próprio (o do Android Studio, por exemplo); vazia
    /// significa que ele está usando o que veio com a Unity.
    /// </summary>
    static string ResolveSdkPath()
    {
        string custom = EditorPrefs.GetString("AndroidSdkRoot");
        if (!string.IsNullOrEmpty(custom) && Directory.Exists(custom))
            return custom;

        return Path.Combine(
            EditorApplication.applicationContentsPath,
            "PlaybackEngines", "AndroidPlayer", "SDK");
    }

    static int[] InstalledLevels(string platformsFolder)
    {
        if (!Directory.Exists(platformsFolder))
            return new int[0];

        return Directory.GetDirectories(platformsFolder)
            .Select(Path.GetFileName)
            .Where(name => name.StartsWith("android-"))
            .Select(name => int.TryParse(name.Substring("android-".Length), out int level) ? level : -1)
            .Where(level => level > 0)
            .OrderBy(level => level)
            .ToArray();
    }

    static string DescribeTarget()
    {
        var target = PlayerSettings.Android.targetSdkVersion;

        // 0 é o "Automatic (highest installed)" do Inspector, e é o valor de hoje.
        return target == AndroidSdkVersions.AndroidApiLevelAuto
            ? "Automatic (a mais alta instalada)"
            : ((int)target).ToString();
    }

    static string FindSdkmanager(string sdk)
    {
        string[] candidates =
        {
            Path.Combine(sdk, "cmdline-tools", "latest", "bin", "sdkmanager.bat"),
            Path.Combine(sdk, "cmdline-tools", "bin", "sdkmanager.bat"),
            Path.Combine(sdk, "tools", "bin", "sdkmanager.bat"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
