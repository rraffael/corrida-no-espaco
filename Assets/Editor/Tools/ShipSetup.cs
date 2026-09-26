using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cria as fichas das naves em <c>Assets/Ships/</c>, os poderes ativos delas em
/// <c>Assets/Poderes/</c>, e o catálogo em <c>Assets/Resources/</c> que o menu lê.
///
/// Depois disto, nave é um arquivo de dados: mexer nos números é editar no
/// Inspector, e uma terceira nave é um terceiro arquivo mais uma linha no
/// catálogo.
///
/// Roda sem apagar o que já existe: se você tiver ajustado os números à mão,
/// rodar de novo não encosta neles.
/// </summary>
static class ShipSetup
{
    public const string ShipFolder = "Assets/Ships";
    public const string AbilityFolder = "Assets/Poderes";
    public const string StarterShipPath = ShipFolder + "/NaveInicial.asset";
    public const string CatalogPath = LevelSetup.ResourcesFolder + "/ShipCatalog.asset";

    [MenuItem(ProjectTools.ShipsItem, false, 105)]
    internal static void Setup()
    {
        var ships = GetOrCreateAll();
        var catalog = GetOrCreateCatalog(ships);
        GetOrCreateEvolutionModel();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ProjectTools.MarkRun(ProjectTools.ShipsId);

        Debug.Log($"[Naves] {ships.Count} ficha(s) em {ShipFolder} e o catálogo em {CatalogPath}. " +
                  "A aba \"Naves\" do menu lê daqui, e a nave escolhida é a que entra na corrida — " +
                  "a ficha ligada na cena virou só o plano B de quem abre a Game.unity direto.",
                  catalog);
    }

    /// <summary>
    /// As naves do jogo. Cada uma preenche **só o que a diferencia**, deixando o
    /// resto nos valores de fábrica do <see cref="ShipDefinition"/> — que são os
    /// números com que o jogo foi equilibrado e aprovado no aparelho em 21/08.
    ///
    /// **Todas têm os mesmos atributos, e é de propósito por enquanto:** a
    /// diferença entre elas é o poder, e só. Mexer nos números junto misturaria
    /// duas variáveis no mesmo teste, e o que se quer saber agora é se os poderes
    /// se sustentam.
    /// </summary>
    internal static List<ShipDefinition> GetOrCreateAll()
    {
        Directory.CreateDirectory(ShipFolder);
        Directory.CreateDirectory(AbilityFolder);
        AssetDatabase.Refresh();

        var homing = GetOrCreatePower<HomingShotsAbility>("Poder-TirosTeleguiados", ability =>
        {
            ability.displayName = "Tiros teleguiados";
            ability.description = "Os tiros perseguem os obstáculos, esteja a nave na faixa que estiver.";
            ability.lifetime = ShipAbility.Lifetime.PorTempo;
            ability.durationSeconds = 3f;
            ability.cooldownSeconds = 30f;
            ability.usesPerRace = 0; // Sem limite: quem segura é a recarga.
            ability.color = new Color(0.55f, 0.95f, 1f, 1f);
        });

        var autopilot = GetOrCreatePower<AutopilotAbility>("Poder-SuperIA", ability =>
        {
            ability.displayName = "Super IA";
            ability.description = "O jogo assume a nave e desvia sozinho dos obstáculos.";
            ability.lifetime = ShipAbility.Lifetime.PorTempo;
            ability.durationSeconds = 3f;
            ability.cooldownSeconds = 0f; // Irrelevante: a carga é uma só e não volta.
            ability.usesPerRace = 1;
            ability.color = new Color(1f, 0.78f, 0.4f, 1f);
        });

        var reinforcement = GetOrCreatePower<StructuralReinforcementConditional>(
            "Condicional-ReforcoEstrutural", conditional =>
            {
                conditional.displayName = "Reforço estrutural";
                conditional.description = "Cada abate soma 4% em defesa, dano, cadência e " +
                                          "aceleração, até 40%.";
                conditional.trigger = "a cada abate, até 10 cargas";
                conditional.percentPerKill = 4f;
                conditional.maxPercent = 40f;
                conditional.stageCounts = new[] { 5, 10 };
                conditional.color = new Color(0.95f, 0.35f, 0.3f, 1f);
            });

        var ships = new List<ShipDefinition>
        {
            GetOrCreateShip("Nave-Nay", ship =>
            {
                ship.displayName = "Nay";
                ship.description = "Boa de tiro. O poder dela resolve o problema de estar na faixa errada.";
                ship.color = new Color(0.55f, 0.95f, 1f, 1f);
                ship.intrinsicAbility = homing;
            }),

            GetOrCreateShip("Nave-Raffa", ship =>
            {
                ship.displayName = "Raffa";
                ship.description = "Carrega uma carta só, e ela vale a corrida inteira se for jogada na hora certa.";
                ship.color = new Color(1f, 0.78f, 0.4f, 1f);
                ship.intrinsicAbility = autopilot;
            }),

            // Atributos de fábrica, como as outras: a lista de 26/09/2026 trouxe o
            // poder, não os números. A diferença dela está toda no Reforço.
            GetOrCreateShip("Nave-Predadora", ship =>
            {
                ship.displayName = "Predadora";
                ship.description = "Fica mais forte a cada abate. Quanto mais caça, mais perigosa.";
                ship.color = new Color(0.95f, 0.35f, 0.3f, 1f);
                ship.conditionalAbility = reinforcement;
            }),
        };

        // A ficha antiga continua existindo e entra no catálogo como as outras:
        // ela é a nave com que o jogo foi aprovado no aparelho, e é a única sem
        // poder nenhum — serve de régua para saber quanto os poderes mudam.
        var starter = GetOrCreateStarter();
        if (string.IsNullOrEmpty(starter.description))
        {
            starter.description = "A nave de sempre, sem poder. É com ela que o jogo foi equilibrado.";
            EditorUtility.SetDirty(starter);
        }

        ships.Insert(0, starter);
        return ships;
    }

    /// <summary>
    /// A ficha inicial. **Não preenche número nenhum de propósito:** os valores
    /// de fábrica do <see cref="ShipDefinition"/> já são os números com que o
    /// jogo foi equilibrado e aprovado no aparelho.
    /// </summary>
    internal static ShipDefinition GetOrCreateStarter()
    {
        var existing = AssetDatabase.LoadAssetAtPath<ShipDefinition>(StarterShipPath);
        if (existing != null)
            return existing;

        // A pasta é criada aqui, e não só no Setup: o BattleSetup chama este
        // método direto, e nesse caminho ninguém garantiu que ela existe.
        Directory.CreateDirectory(ShipFolder);
        AssetDatabase.Refresh();

        var created = ScriptableObject.CreateInstance<ShipDefinition>();
        created.displayName = "Nave inicial";

        AssetDatabase.CreateAsset(created, StarterShipPath);
        return created;
    }

    static ShipDefinition GetOrCreateShip(string fileName, System.Action<ShipDefinition> fill)
    {
        string path = $"{ShipFolder}/{fileName}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<ShipDefinition>(path);
        if (existing != null)
            return existing;

        var created = ScriptableObject.CreateInstance<ShipDefinition>();
        fill(created);

        AssetDatabase.CreateAsset(created, path);
        newlyCreated.Add(created);
        return created;
    }

    /// <summary>
    /// As naves que esta montagem criou do zero. Só elas entram num catálogo que
    /// já tinha lista — ver <see cref="GetOrCreateCatalog"/>.
    /// </summary>
    static readonly List<ShipDefinition> newlyCreated = new List<ShipDefinition>();

    /// <summary>
    /// A ficha do poder — ativo ou condicional —, criada se faltar e
    /// **recalibrada de qualquer jeito**.
    ///
    /// É a mesma regra dos modificadores de fase, e pelo mesmo motivo: recarga e
    /// duração são números de equilíbrio, decididos no aparelho e ajustados por
    /// pedido. A ficha da **nave** continua sendo escrita uma vez só — nela mora
    /// nome e identidade, que são do Raffael.
    ///
    /// Reescreve o conteúdo do mesmo arquivo, nunca cria outro: o GUID tem de
    /// ficar, senão a nave perde o poder dela.
    /// </summary>
    static T GetOrCreatePower<T>(string fileName, System.Action<T> fill) where T : ScriptableObject
    {
        string path = $"{AbilityFolder}/{fileName}.asset";

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

    public const string EvolutionModelPath =
        LevelSetup.ResourcesFolder + "/" + ShipEvolutionModel.ResourcePath + ".asset";

    /// <summary>
    /// O molde de evolução padrão, em Resources. **Criado uma vez e nunca
    /// reescrito:** os números dele são os valores de fábrica da classe, e
    /// repeti-los aqui criaria dois lugares para manter iguais. Ajuste de custo
    /// ou de alvo se faz no Inspector do asset, e sobrevive ao Montar.
    /// </summary>
    static void GetOrCreateEvolutionModel()
    {
        if (AssetDatabase.LoadAssetAtPath<ShipEvolutionModel>(EvolutionModelPath) != null)
            return;

        Directory.CreateDirectory(LevelSetup.ResourcesFolder);
        AssetDatabase.Refresh();
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ShipEvolutionModel>(), EvolutionModelPath);
    }

    /// <summary>
    /// O catálogo, em Resources para o menu achar sem referência de cena.
    ///
    /// **Lista vazia ganha todas; lista que já existe ganha só as naves que esta
    /// montagem acabou de criar**, no fim. Assim tirar uma nave à mão não é
    /// desfeito na próxima montagem — a ficha dela já existe, então ela não é
    /// nova —, e uma nave nova aparece no menu sem ninguém editar o catálogo.
    /// </summary>
    static ShipCatalog GetOrCreateCatalog(List<ShipDefinition> ships)
    {
        var fresh = new List<ShipDefinition>(newlyCreated);
        newlyCreated.Clear();

        Directory.CreateDirectory(LevelSetup.ResourcesFolder);
        AssetDatabase.Refresh();

        var catalog = AssetDatabase.LoadAssetAtPath<ShipCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ShipCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        if (catalog.ships == null || catalog.ships.Length == 0)
        {
            catalog.ships = ships.ToArray();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        var listed = new List<ShipDefinition>(catalog.ships);
        foreach (var ship in fresh)
        {
            if (!listed.Contains(ship))
                listed.Add(ship);
        }

        if (listed.Count != catalog.ships.Length)
        {
            catalog.ships = listed.ToArray();
            EditorUtility.SetDirty(catalog);
        }

        return catalog;
    }
}
