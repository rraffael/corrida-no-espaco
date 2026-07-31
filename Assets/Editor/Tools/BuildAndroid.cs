using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds de Android pelo menu ou por linha de comando.
///
/// O keystore nunca fica no projeto nem no repositório: caminho e senhas vêm de
/// variáveis de ambiente, lidas só na hora do build de release. Assim o
/// ProjectSettings.asset continua limpo e a chave não viaja no git.
///
/// Por linha de comando:
///   Unity.exe -quit -batchmode -projectPath "C:\Projeto Pessoal\Jogo" -executeMethod BuildAndroid.Release
///
/// Variáveis usadas pelo release (todas obrigatórias):
///   CNE_KEYSTORE_PATH   caminho do .keystore
///   CNE_KEYSTORE_PASS   senha do keystore
///   CNE_KEY_ALIAS       alias da chave
///   CNE_KEY_ALIAS_PASS  senha do alias
/// Opcional: CNE_BUILD_OUTPUT (arquivo de saída; se ausente, cai em Builds/).
/// </summary>
static class BuildAndroid
{
    const string OutputFolder = "Builds";

    [MenuItem("Tools/Corrida no Espaço/Build/APK de teste", false, 200)]
    static void DevelopmentApkFromMenu()
    {
        Run(appBundle: false, release: false);
    }

    [MenuItem("Tools/Corrida no Espaço/Build/AAB de release", false, 201)]
    static void ReleaseBundleFromMenu()
    {
        Run(appBundle: true, release: true);
    }

    /// <summary>Alvo do -executeMethod: AAB assinado, para subir na Play.</summary>
    public static void Release()
    {
        Exit(Run(appBundle: true, release: true));
    }

    /// <summary>Alvo do -executeMethod: APK de desenvolvimento, para instalar direto.</summary>
    public static void Development()
    {
        Exit(Run(appBundle: false, release: false));
    }

    static bool Run(bool appBundle, bool release)
    {
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[Build] Nenhuma cena habilitada em File > Build Profiles. Build cancelado.");
            return false;
        }

        if (release && !TryConfigureSigning())
            return false;

        if (!release)
            ClearSigning();

        EditorUserBuildSettings.buildAppBundle = appBundle;

        string output = ResolveOutputPath(appBundle, release);
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = release ? BuildOptions.None : BuildOptions.Development
        };

        Debug.Log($"[Build] {(release ? "Release" : "Desenvolvimento")} " +
                  $"{(appBundle ? "AAB" : "APK")} → {output}");
        Debug.Log($"[Build] Cenas: {string.Join(", ", scenes)}");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        // As senhas ficam na memória do Editor depois do build; limpar evita que
        // um build manual seguinte assine sem querer com a chave de release.
        if (release)
            ClearSigning();

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[Build] Falhou: {summary.result}, {summary.totalErrors} erro(s).");
            return false;
        }

        Debug.Log($"[Build] OK em {summary.totalTime:hh\\:mm\\:ss}, " +
                  $"{summary.totalSize / (1024f * 1024f):F1} MB: {output}");
        return true;
    }

    static bool TryConfigureSigning()
    {
        string keystore = Environment.GetEnvironmentVariable("CNE_KEYSTORE_PATH");
        string keystorePass = Environment.GetEnvironmentVariable("CNE_KEYSTORE_PASS");
        string alias = Environment.GetEnvironmentVariable("CNE_KEY_ALIAS");
        string aliasPass = Environment.GetEnvironmentVariable("CNE_KEY_ALIAS_PASS");

        if (string.IsNullOrEmpty(keystore) || string.IsNullOrEmpty(keystorePass) ||
            string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(aliasPass))
        {
            Debug.LogError(
                "[Build] Release exige as variáveis de ambiente CNE_KEYSTORE_PATH, " +
                "CNE_KEYSTORE_PASS, CNE_KEY_ALIAS e CNE_KEY_ALIAS_PASS. " +
                "Defina-as antes de abrir a Unity — o Editor só lê o ambiente na inicialização.");
            return false;
        }

        if (!File.Exists(keystore))
        {
            Debug.LogError($"[Build] Keystore não encontrado: {keystore}");
            return false;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystore;
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasName = alias;
        PlayerSettings.Android.keyaliasPass = aliasPass;
        return true;
    }

    /// <summary>
    /// Devolve o projeto ao estado "sem chave nenhuma", que é como ele fica
    /// versionado. Sem isso o caminho absoluto do keystore volta a vazar para o
    /// ProjectSettings.asset de quem rodar um build.
    /// </summary>
    static void ClearSigning()
    {
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.Android.keystoreName = string.Empty;
        PlayerSettings.Android.keystorePass = string.Empty;
        PlayerSettings.Android.keyaliasName = string.Empty;
        PlayerSettings.Android.keyaliasPass = string.Empty;
    }

    static string ResolveOutputPath(bool appBundle, bool release)
    {
        string fromEnvironment = Environment.GetEnvironmentVariable("CNE_BUILD_OUTPUT");
        if (!string.IsNullOrEmpty(fromEnvironment))
            return fromEnvironment;

        string extension = appBundle ? "aab" : "apk";
        string kind = release ? "release" : "dev";
        string version = PlayerSettings.bundleVersion;
        int code = PlayerSettings.Android.bundleVersionCode;
        string name = $"{PlayerSettings.productName}-{version}-{code}-{kind}.{extension}";

        // Builds/ já está no .gitignore, então o artefato não entra no repositório.
        return Path.Combine(Directory.GetCurrentDirectory(), OutputFolder, name);
    }

    static void Exit(bool success)
    {
        // Só encerra o processo quando veio da linha de comando; pelo menu isso
        // fecharia a Unity na cara do usuário.
        if (Application.isBatchMode)
            EditorApplication.Exit(success ? 0 : 1);
    }
}
