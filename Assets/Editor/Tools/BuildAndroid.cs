using System;
using System.IO;
using System.Linq;
using Unity.Android.Types;
using UnityEditor;
using UnityEditor.Android;
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

    [MenuItem(ProjectTools.BuildApkItem, false, 200)]
    static void DevelopmentApkFromMenu()
    {
        Run(appBundle: false, release: false);
    }

    [MenuItem(ProjectTools.BuildAabItem, false, 201)]
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

        // Símbolos de depuração no build de release. Sem eles, o relatório de
        // travamento da Play chega como um monte de endereço em hexadecimal, e
        // não dá para saber sequer em que script o jogo morreu — o IL2CPP
        // compila para código nativo, então o nome do método some.
        //
        // Passou a importar em 21/08/2026, quando entrou gente no teste interno:
        // até então quem travava era o Raffael, com o cabo na mão e o logcat
        // aberto. Agora o travamento acontece longe, e o que sobra é o relatório.
        //
        // **`SymbolTable` e não `Full`:** a tabela traz os nomes dos métodos, que
        // é o que a Play precisa para desembaralhar a pilha, e evita o pacote
        // gigante do `Full`, que só serve para depurar com ferramenta nativa
        // anexada. O arquivo de símbolos sai ao lado do `.aab` e **é subido à
        // parte** no Play Console — ele não vai dentro do pacote.
        //
        // Esta é a API da Unity 6; o `EditorUserBuildSettings.androidCreateSymbols`
        // de antes ficou obsoleto.
        UserBuildSettings.DebugSymbols.level =
            release ? DebugSymbolLevel.SymbolTable : DebugSymbolLevel.None;
        UserBuildSettings.DebugSymbols.format = DebugSymbolFormat.Zip;

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

        if (release)
            ReportSymbols(output);

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

    /// <summary>
    /// Diz no Console onde ficou o pacote de símbolos, e avisa se ele não saiu.
    ///
    /// Existe porque o arquivo é **fácil de esquecer**: ele não vai dentro do
    /// `.aab`, sai ao lado dele, e sobe à parte no Play Console. Um release
    /// enviado sem os símbolos só cobra o preço meses depois, quando chega o
    /// primeiro relatório de travamento ilegível — e aí é tarde, porque a versão
    /// que travou já está na mão das pessoas.
    /// </summary>
    static void ReportSymbols(string output)
    {
        string folder = Path.GetDirectoryName(output);
        if (string.IsNullOrEmpty(folder))
            return;

        // A Unity nomeia o pacote a partir do nome do build, mas o sufixo já
        // mudou entre versões (.symbols.zip, -1.0-v1.symbols.zip). Procurar pelo
        // padrão é mais estável do que remontar o nome à mão.
        string prefix = Path.GetFileNameWithoutExtension(output);
        var found = Directory.GetFiles(folder, prefix + "*symbols*.zip");

        if (found.Length == 0)
        {
            Debug.LogWarning("[Build] Os símbolos de depuração não foram encontrados ao lado do " +
                             "pacote. Sem eles, relatório de travamento da Play chega ilegível. " +
                             "Confira Player Settings > Publishing Settings > Debugging.");
            return;
        }

        foreach (var file in found)
        {
            var info = new FileInfo(file);
            Debug.Log($"[Build] Símbolos de depuração: {file} " +
                      $"({info.Length / (1024f * 1024f):F1} MB). **Subir à parte** no Play " +
                      "Console, em Versões > a versão > Símbolos de depuração.");
        }
    }

    static void Exit(bool success)
    {
        // Só encerra o processo quando veio da linha de comando; pelo menu isso
        // fecharia a Unity na cara do usuário.
        if (Application.isBatchMode)
            EditorApplication.Exit(success ? 0 : 1);
    }
}
