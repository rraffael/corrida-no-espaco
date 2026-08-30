using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cria as fichas dos **modificadores de fase** em <c>Assets/Modificadores/</c> e
/// as põe no repertório das fases do catálogo.
///
/// São três, um por categoria, e é assim de propósito: o objetivo desta primeira
/// leva é **provar a arquitetura**, não encher o jogo. Um Reforço, um
/// Debilitante e um Especial exercitam os três caminhos que qualquer modificador
/// futuro vai usar — mexer num atributo para cima, para baixo, e mexer em algo
/// que não é atributo nenhum.
///
/// Roda sem apagar o que já existe: se você tiver ajustado os números à mão,
/// rodar de novo não encosta neles.
/// </summary>
static class ModifierSetup
{
    public const string Folder = "Assets/Modificadores";

    [MenuItem(ProjectTools.ModifiersItem, false, 106)]
    internal static void Setup()
    {
        var created = GetOrCreateAll();
        AttachToLevels(created);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ProjectTools.MarkRun(ProjectTools.ModifiersId);

        Debug.Log($"[Modificadores] {created.Count} ficha(s) em {Folder}, e o repertório " +
                  "das fases do catálogo apontando para elas. Os itens nascem na pista pelo " +
                  "LevelModifierSpawner, respeitando a mesma regra de faixa livre do obstáculo.");
    }

    /// <summary>
    /// As três fichas. Cada uma preenche **só o que a diferencia** — nome,
    /// categoria e duração —, deixando o resto nos valores de fábrica da classe:
    /// duplicar aqui o que já está lá criaria dois lugares para manter iguais, e
    /// um deles ia ficar para trás.
    ///
    /// <para>
    /// ⚠️ **Esta receita é reaplicada a cada montagem, e sobrescreve o que
    /// estiver no arquivo** — ao contrário das fichas de fase e de nave, que só
    /// se escrevem uma vez.
    /// </para>
    ///
    /// A diferença tem motivo. As fichas que não se sobrescrevem são as que o
    /// Raffael afina no Inspector; **estas três são de equilíbrio, e o equilíbrio
    /// dos modificadores é decidido no aparelho e volta como pedido**: "reduz o
    /// tempo lento em 25%", "as armas por 3,5 s". Se a receita não pudesse
    /// reescrever, cada ajuste desses seria pedir para ele abrir três arquivos e
    /// digitar número — e o número certo ficaria só no arquivo dele, longe do
    /// histórico.
    ///
    /// **A consequência a saber:** afinar um destes no Inspector vale até o
    /// próximo Montar. Gostou de um número? Peça para ele entrar aqui.
    /// </summary>
    internal static List<LevelModifier> GetOrCreateAll()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();

        var all = new List<LevelModifier>();

        all.Add(GetOrCreate<AttackSpeedBoost>("Reforco-CadenciaDobrada", modifier =>
        {
            modifier.displayName = "Aumento de Velocidade de Tiro";
            modifier.description = "A nave atira o dobro de rápido.";
            modifier.category = LevelModifier.Category.Reforco;
            modifier.lifetime = LevelModifier.Lifetime.PorTempo;
            modifier.durationSeconds = 3f;
        }));

        all.Add(GetOrCreate<WeaponsOffline>("Debilitante-ArmasDesativadas", modifier =>
        {
            modifier.displayName = "Desabilitar Armas";
            modifier.description = "A nave para de atirar.";
            modifier.category = LevelModifier.Category.Debilitante;
            modifier.lifetime = LevelModifier.Lifetime.PorTempo;

            // 3,5 s e não 5: julgado no aparelho em 30/08/2026. Cinco segundos
            // sem arma era tempo demais parado vendo obstáculo passar.
            modifier.durationSeconds = 3.5f;
        }));

        all.Add(GetOrCreate<TimeSlow>("Especial-TempoLento", modifier =>
        {
            modifier.displayName = "Redução na Velocidade do Tempo";
            modifier.description = "Tudo em volta passa a andar em metade da velocidade.";
            modifier.category = LevelModifier.Category.Especial;
            modifier.lifetime = LevelModifier.Lifetime.PorTempo;

            // 2,75 s DE JOGO, que a meia velocidade viram 5,5 de relógio de
            // parede — ver TimeSlow. A calibragem foi de ida e volta no aparelho:
            // 3 no papel, 2,25 depois do primeiro teste, e 2,75 depois do
            // segundo. O primeiro corte foi grande porque o efeito era invisível
            // e por isso parecia arrastado; com o véu azulado avisando, deu para
            // devolver metade do que se tinha tirado.
            modifier.durationSeconds = 2.75f;
        }));

        return all;
    }

    /// <summary>
    /// Cria a ficha se não houver, e **reaplica a receita de qualquer jeito** —
    /// ver o aviso em <see cref="GetOrCreateAll"/>. O arquivo é sempre o mesmo, e
    /// só o conteúdo é reescrito, para o GUID não mudar e nada que aponte para
    /// ele se perder.
    /// </summary>
    static LevelModifier GetOrCreate<T>(string fileName, System.Action<T> fill)
        where T : LevelModifier
    {
        string path = $"{Folder}/{fileName}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null)
        {
            fill(existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var created = ScriptableObject.CreateInstance<T>();
        fill(created);

        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    /// <summary>
    /// Põe as três no repertório de todas as fases, inclusive a sem fim.
    ///
    /// **Todas as fases com todos os modificadores, por enquanto** — quais entram
    /// em qual fase é decisão de projeto ainda em aberto, e é a ficha da fase que
    /// vai responder. O que importa hoje é que o campo existe e é lido: quando a
    /// resposta chegar, ela é edição de asset, não código.
    ///
    /// **Só preenche fase que ainda não tem nenhum.** Assim, tirar um modificador
    /// de uma fase à mão não é desfeito na próxima montagem.
    /// </summary>
    static void AttachToLevels(List<LevelModifier> modifiers)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelSetup.CatalogPath);
        if (catalog == null)
        {
            Debug.LogWarning("[Modificadores] Não achei o catálogo de fases. As fichas foram " +
                             "criadas, mas nenhuma fase as usa ainda — rode antes o 'Criar fases " +
                             "e fichas de obstáculo'.");
            return;
        }

        foreach (var level in AllLevels(catalog))
        {
            if (level == null || (level.modifiers != null && level.modifiers.Length > 0))
                continue;

            level.modifiers = modifiers.ToArray();
            EditorUtility.SetDirty(level);
        }
    }

    static IEnumerable<LevelDefinition> AllLevels(LevelCatalog catalog)
    {
        foreach (var level in catalog.levels)
            yield return level;

        yield return catalog.endlessLevel;
    }
}
