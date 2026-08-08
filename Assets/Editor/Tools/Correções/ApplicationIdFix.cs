using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Conserto pontual: devolve o `applicationIdentifier` do Android ao valor
/// definitivo do projeto.
///
/// **O que aconteceu, em 08/08/2026.** O `ReleaseSettingsFix` mudou o
/// `productName` para "Corrida no Espaço" e não reafirmou o pacote. A Unity
/// deriva o identificador de `com.&lt;companyName&gt;.&lt;productName&gt;` enquanto
/// ele não for imposto de novo, então trocar o nome do produto **regerou o
/// pacote**: `br.com.raffael.corridanoespaco` virou `com.Raffael.CorridanoEspao`,
/// e o `.aab` saiu com o pacote errado. A Play recusou o upload, que é o único
/// motivo de o estrago ter parado aí.
///
/// **A ordem importa:** o identificador tem de ser escrito **depois** do
/// `productName`, nunca antes.
///
/// Some depois de rodar. O `PreflightCheck` passou a exigir este valor exato,
/// então o mesmo erro não volta calado.
/// </summary>
static class ApplicationIdFix
{
    [MenuItem(ProjectTools.ApplicationIdItem, false, 200)]
    internal static void Run()
    {
        string previous = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);

        if (previous == ProjectIdentity.ApplicationId)
        {
            Debug.Log($"[Pacote] Já está correto: {ProjectIdentity.ApplicationId}. Nada a fazer.");
            ProjectTools.MarkRun(ProjectTools.ApplicationIdId);
            return;
        }

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ProjectIdentity.ApplicationId);
        AssetDatabase.SaveAssets();

        ProjectTools.MarkRun(ProjectTools.ApplicationIdId);

        Debug.Log(
            $"[Pacote] Corrigido: \"{previous}\" → \"{ProjectIdentity.ApplicationId}\".\n\n" +
            "  O .aab em Builds/ foi gerado com o pacote errado e não serve — **gere outro**.\n" +
            "  O version code pode continuar 1: nada chegou a ser aceito pela Play, então o " +
            "número ainda está livre.");
    }
}
