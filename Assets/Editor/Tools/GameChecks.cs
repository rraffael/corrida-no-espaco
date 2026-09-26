using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// As **verificações automáticas** do jogo *(pedido do Raffael, 26/09/2026)*:
/// contas que precisam fechar sempre — a escadinha de evolução, os tetos, a
/// regra de soma dos bônus, os limites dos atributos e a integridade das
/// fichas. Pegam erro de conta antes de ele chegar ao aparelho.
///
/// **Rodam sozinhas depois de cada compilação** e cabem numa linha quando tudo
/// passa; quando algo quebra, sai um erro no Console dizendo o quê. O item de
/// menu mostra o relatório inteiro, com os números de pior caso.
///
/// **Por que não o Test Runner da Unity:** o pacote de testes não está no
/// projeto, e instalá-lo é mexer em pacote — decisão do Raffael. Isto aqui é C#
/// puro, sem dependência, e lê as fichas de verdade do jogo: nave nova no
/// catálogo passa pelas mesmas verificações sem ninguém escrever teste novo.
/// </summary>
[InitializeOnLoad]
static class GameChecks
{
    const float Tolerance = 0.0001f;

    static GameChecks()
    {
        // Depois que o Editor terminou de carregar: antes disso o catálogo pode
        // ainda não estar importado, e a verificação acusaria o que não é erro.
        EditorApplication.delayCall += RunQuietly;
    }

    static void RunQuietly()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var report = RunAll();
        if (report.Failures.Count > 0)
            Debug.LogError(report.Describe(full: false));
        else if (report.Warnings.Count > 0)
            Debug.LogWarning(report.Describe(full: false));
        else
            Debug.Log($"[Verificações] {report.Passed} de {report.Passed} passaram.");
    }

    [MenuItem(ProjectTools.ChecksItem, false, 160)]
    static void RunFromMenu()
    {
        var report = RunAll();
        string text = report.Describe(full: true);

        if (report.Failures.Count > 0)
            Debug.LogError(text);
        else if (report.Warnings.Count > 0)
            Debug.LogWarning(text);
        else
            Debug.Log(text);
    }

    // ── Relatório ────────────────────────────────────────────────────────

    class Report
    {
        public int Passed;
        public readonly List<string> Failures = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> Notes = new List<string>();

        public void Check(bool ok, string what)
        {
            if (ok)
                Passed++;
            else
                Failures.Add(what);
        }

        public void Near(float actual, float expected, string what) =>
            Check(Mathf.Abs(actual - expected) <= Tolerance * Mathf.Max(1f, Mathf.Abs(expected)),
                  $"{what}: deu {actual:0.####}, esperado {expected:0.####}");

        public void Warn(bool ok, string what)
        {
            if (!ok)
                Warnings.Add(what);
        }

        public string Describe(bool full)
        {
            var text = new StringBuilder();
            int total = Passed + Failures.Count;
            text.Append($"[Verificações] {Passed} de {total} passaram");
            if (Warnings.Count > 0)
                text.Append($", {Warnings.Count} aviso(s)");
            text.AppendLine(".");

            foreach (var failure in Failures)
                text.AppendLine("  ✗ " + failure);
            foreach (var warning in Warnings)
                text.AppendLine("  ⚠ " + warning);

            if (full && Notes.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("Pior caso conhecido, para balancear:");
                foreach (var note in Notes)
                    text.AppendLine("  · " + note);
            }

            return text.ToString();
        }
    }

    static Report RunAll()
    {
        var report = new Report();

        try
        {
            var ships = CatalogShips(report);

            CheckLadder(report, ShipEvolutionModel.Default, "molde padrão");
            foreach (var ship in ships)
            {
                if (ship.evolutionModel != null)
                    CheckLadder(report, ship.evolutionModel, $"molde da {ship.displayName}");
            }

            foreach (var ship in ships)
            {
                CheckEvolution(report, ship);
                CheckNoMultiplying(report, ship);
                CheckWorstCase(report, ship);
                CheckTiers(report, ship);
            }

            CheckSumRule(report);
            CheckClamps(report);
            CheckPowerFieldNames(report);
            CheckLevels(report);
            CheckSaveRoundTrip(report);
        }
        catch (Exception e)
        {
            // Uma verificação que estoura não pode esconder as outras nem passar
            // em silêncio: vira falha com o motivo.
            report.Failures.Add($"a verificação estourou: {e.GetType().Name}: {e.Message}");
        }

        return report;
    }

    static List<ShipDefinition> CatalogShips(Report report)
    {
        var ships = new List<ShipDefinition>();
        var catalog = AssetDatabase.LoadAssetAtPath<ShipCatalog>(ShipSetup.CatalogPath);
        report.Check(catalog != null, $"catálogo de naves em {ShipSetup.CatalogPath}");
        if (catalog == null)
            return ships;

        var names = new HashSet<string>();
        foreach (var ship in catalog.ships)
        {
            report.Check(ship != null, "catálogo de naves sem espaço vazio");
            if (ship == null)
                continue;

            report.Check(names.Add(ship.name), $"nave {ship.name} aparece uma vez só no catálogo");
            ships.Add(ship);
        }

        report.Warn(AssetDatabase.LoadAssetAtPath<ShipEvolutionModel>(ShipSetup.EvolutionModelPath) != null,
                    "o molde de evolução não está em Resources — rode o Montar");

        return ships;
    }

    // ── Escadinha ────────────────────────────────────────────────────────

    /// <summary>
    /// A escadinha fecha: todo nível que não é patamar sobe um atributo, os
    /// patamares ficam fora, e cada atributo sobe o mesmo número de vezes (ou
    /// um a mais, quando a conta não é redonda).
    /// </summary>
    static void CheckLadder(Report report, ShipEvolutionModel model, string name)
    {
        report.Check(1 < model.upgrade1Level && model.upgrade1Level < model.upgrade2Level &&
                     model.upgrade2Level < model.passiveLevel && model.passiveLevel <= model.maxLevel,
                     $"{name}: patamares em ordem e dentro do nível máximo " +
                     $"({model.upgrade1Level}, {model.upgrade2Level}, {model.passiveLevel} de {model.maxLevel})");

        model.Ladder(out var levels, out var stats);

        int expected = 0;
        for (int level = 2; level <= model.maxLevel; level++)
        {
            if (model.TierOpenedAt(level) == 0)
                expected++;
        }

        report.Check(levels.Length == expected,
                     $"{name}: {expected} níveis de atributo, a escadinha tem {levels.Length}");

        foreach (int level in levels)
            report.Check(model.TierOpenedAt(level) == 0, $"{name}: nível de patamar {level} fora da escadinha");

        int order = model.ladderOrder != null ? model.ladderOrder.Length : 0;
        report.Check(order > 0, $"{name}: a ordem da escadinha tem atributos");
        if (order == 0)
            return;

        int least = levels.Length / order;
        foreach (var stat in model.ladderOrder)
        {
            int count = 0;
            foreach (var step in stats)
            {
                if (step == stat)
                    count++;
            }

            report.Check(count == least || count == least + 1,
                         $"{name}: {ShipEvolution.Name(stat)} sobe {count} vez(es), esperado {least}");
        }
    }

    // ── Evolução de cada nave ────────────────────────────────────────────

    static readonly ShipStat[] AllStats = (ShipStat[])Enum.GetValues(typeof(ShipStat));

    static float Factory(ShipDefinition ship, ShipStat stat)
    {
        switch (stat)
        {
            case ShipStat.CruiseSpeed: return ship.CruiseSpeed;
            case ShipStat.Acceleration: return ship.Acceleration;
            case ShipStat.Damage: return ship.Damage;
            case ShipStat.AttackSpeed: return ship.AttackSpeed;
            case ShipStat.MaxHealth: return ship.MaxHealth;
            case ShipStat.DefensePercent: return ship.DefensePercent;
            case ShipStat.KillSpeedGain: return ship.KillSpeedGain;
            default: return ship.CrashCost;
        }
    }

    /// <summary>
    /// Nível 1 é a nave de fábrica, o máximo bate nos tetos do molde, nada
    /// desce no caminho, e cada nível sobe exatamente o que a escadinha diz.
    /// </summary>
    static void CheckEvolution(Report report, ShipDefinition ship)
    {
        var model = ShipEvolution.Model(ship);
        string who = ship.displayName;
        int max = model.maxLevel;

        foreach (var stat in AllStats)
        {
            report.Near(ShipEvolution.Value(ship, stat, 1), Factory(ship, stat),
                        $"{who}: {ShipEvolution.Name(stat)} no nível 1 é o de fábrica");
            report.Near(ShipEvolution.Bonus(ship, stat, 1), 0f,
                        $"{who}: bônus de {ShipEvolution.Name(stat)} no nível 1 é zero");
        }

        report.Near(ShipEvolution.Value(ship, ShipStat.CruiseSpeed, max),
                    ship.CruiseSpeed * model.cruiseSpeedMultiplier, $"{who}: teto do cruzeiro");
        report.Near(ShipEvolution.Value(ship, ShipStat.Acceleration, max),
                    ship.Acceleration * model.accelerationMultiplier, $"{who}: teto da aceleração");
        report.Near(ShipEvolution.Value(ship, ShipStat.Damage, max),
                    ship.Damage * model.damageMultiplier, $"{who}: teto do dano");
        report.Near(ShipEvolution.Value(ship, ShipStat.AttackSpeed, max),
                    ship.AttackSpeed * model.attackSpeedMultiplier, $"{who}: teto da cadência");
        report.Near(ShipEvolution.Value(ship, ShipStat.MaxHealth, max),
                    ship.MaxHealth * model.maxHealthMultiplier, $"{who}: teto da vida");
        report.Near(ShipEvolution.Value(ship, ShipStat.DefensePercent, max),
                    ship.DefensePercent + model.defenseBonusPoints, $"{who}: teto da defesa");
        report.Near(ShipEvolution.Value(ship, ShipStat.KillSpeedGain, max),
                    ship.KillSpeedGain * model.killSpeedGainMultiplier, $"{who}: teto do ganho por abate (total)");
        report.Near(ShipEvolution.Value(ship, ShipStat.CrashCost, max), ship.CrashCost,
                    $"{who}: custo da batida não evolui");

        model.Ladder(out var levels, out var stats);
        var raisedAt = new Dictionary<int, ShipStat>();
        for (int i = 0; i < levels.Length; i++)
            raisedAt[levels[i]] = stats[i];

        int wrongSteps = 0;
        string firstWrong = null;

        for (int level = 2; level <= max; level++)
        {
            bool isStep = raisedAt.TryGetValue(level, out var raised);

            foreach (var stat in AllStats)
            {
                float before = ShipEvolution.Value(ship, stat, level - 1);
                float after = ShipEvolution.Value(ship, stat, level);

                // O abate sobe junto quando a aceleração sobe, porque sai dela.
                bool mayMove = isStep && (stat == raised ||
                                          (raised == ShipStat.Acceleration && stat == ShipStat.KillSpeedGain));
                bool shouldRise = isStep && stat == raised && stat != ShipStat.CrashCost;

                bool ok = after >= before - Tolerance &&
                          (mayMove || Mathf.Abs(after - before) <= Tolerance) &&
                          (!shouldRise || after > before + Tolerance);

                if (ok)
                    continue;

                wrongSteps++;
                firstWrong ??= $"nível {level}, {ShipEvolution.Name(stat)}: {before:0.###} → {after:0.###}";
            }
        }

        report.Check(wrongSteps == 0,
                     $"{who}: cada nível sobe só o atributo da escadinha, e patamar não sobe nenhum " +
                     $"({wrongSteps} erro(s); o primeiro: {firstWrong})");
    }

    /// <summary>
    /// **O bônus de evolução não multiplica os poderes**, nem o contrário. Para
    /// cada atributo, no nível máximo: somar a evolução a um poder de +40% dá
    /// exatamente o poder sozinho mais a evolução sozinha — cada um medido
    /// contra a base de fábrica.
    /// </summary>
    static void CheckNoMultiplying(Report report, ShipDefinition ship)
    {
        int max = ShipEvolution.MaxLevel(ship);
        var power = StatModifier.Times(ShipStat.Acceleration, 1.4f);

        foreach (var stat in AllStats)
        {
            if (stat == ShipStat.KillSpeedGain)
                continue;

            var statPower = StatModifier.Times(stat, 1.4f);
            var evolution = StatModifier.Plus(stat, ShipEvolution.Bonus(ship, stat, max));
            float factory = Factory(ship, stat);

            float both = ShipStats.Combine(new[] { evolution, statPower }, stat, factory);
            float powerOnly = ShipStats.Combine(new[] { statPower }, stat, factory);
            float evolutionOnly = ShipStats.Combine(new[] { evolution }, stat, factory);

            report.Near(both, powerOnly + evolutionOnly - factory,
                        $"{ship.displayName}: evolução e poder de {ShipEvolution.Name(stat)} só se somam");
        }

        // O abate é a cadeia inteira, como a corrida faz: aceleração de agora
        // vezes a fração de fábrica, e o bônus de evolução do abate por cima.
        var accelEvolution = StatModifier.Plus(ShipStat.Acceleration,
                                               ShipEvolution.Bonus(ship, ShipStat.Acceleration, max));
        var killEvolution = StatModifier.Plus(ShipStat.KillSpeedGain,
                                              ShipEvolution.Bonus(ship, ShipStat.KillSpeedGain, max));

        float accelNow = ShipStats.Combine(new[] { accelEvolution, power }, ShipStat.Acceleration, ship.Acceleration);
        float kill = ShipStats.Combine(new[] { killEvolution }, ShipStat.KillSpeedGain,
                                       accelNow * ship.KillGainFactor);

        float expected = ShipEvolution.Value(ship, ShipStat.KillSpeedGain, max) +
                         ship.Acceleration * 0.4f * ship.KillGainFactor;

        report.Near(kill, expected,
                    $"{ship.displayName}: poder de aceleração rende o mesmo abate com ou sem evolução");
    }

    // ── Limites ──────────────────────────────────────────────────────────

    /// <summary>
    /// A regra de soma: a ordem não importa e empilhar não estoura.
    /// </summary>
    static void CheckSumRule(Report report)
    {
        var plus = StatModifier.Plus(ShipStat.Acceleration, 2f);
        var times = StatModifier.Times(ShipStat.Acceleration, 1.5f);

        report.Near(ShipStats.Combine(new[] { plus, times }, ShipStat.Acceleration, 4f), 8f,
                    "regra de soma: +2 e ×1,5 sobre 4 dão 8");
        report.Near(ShipStats.Combine(new[] { times, plus }, ShipStat.Acceleration, 4f), 8f,
                    "regra de soma: a ordem não importa");
        report.Near(ShipStats.Combine(new[] { times, times }, ShipStat.Acceleration, 4f), 8f,
                    "regra de soma: dois ×1,5 dão o dobro da base, e não 2,25 vezes");
    }

    /// <summary>Os tetos e pisos dos atributos seguram qualquer exagero de poder.</summary>
    static void CheckClamps(Report report)
    {
        report.Near(ShipStats.Clamp(ShipStat.DefensePercent, 500f), 99f, "defesa nunca passa de 99%");
        report.Near(ShipStats.Clamp(ShipStat.DefensePercent, -50f), 0f, "defesa nunca fica negativa");
        report.Near(ShipStats.Clamp(ShipStat.AttackSpeed, -3f), 0f, "cadência nunca fica negativa");
        report.Near(ShipStats.Clamp(ShipStat.MaxHealth, -10f), 1f, "vida máxima nunca abaixo de 1");
        report.Near(ShipStats.Clamp(ShipStat.Acceleration, 0f), 0.1f, "aceleração nunca abaixo de 0,1");
        report.Near(ShipStats.Clamp(ShipStat.CruiseSpeed, -5f), 0f, "cruzeiro nunca fica negativo");
    }

    /// <summary>
    /// **O pior caso conhecido** *(ideia do Raffael, 26/09/2026)*: a nave no
    /// nível máximo, com o poder dela no teto e todos os modificadores de fase
    /// de atributo que existem valendo juntos.
    ///
    /// **Erro** é o que quebra o jogo: cruzeiro que passa da dobra venceria a
    /// fase sem jogar. **Aviso** é o que encosta num teto. O resto vira nota no
    /// relatório do menu, para a sessão de balancear.
    /// </summary>
    static void CheckWorstCase(Report report, ShipDefinition ship)
    {
        int max = ShipEvolution.MaxLevel(ship);
        string who = ship.displayName;

        float cruise = ShipEvolution.Value(ship, ShipStat.CruiseSpeed, max);
        float lowestWarp = LowestWarpSpeed();

        if (lowestWarp > 0f)
        {
            report.Check(cruise < lowestWarp,
                         $"{who}: cruzeiro no nível máximo ({Shown(cruise)}) abaixo da dobra mais baixa " +
                         $"({Shown(lowestWarp)}) — senão a fase se vence só esperando");

            report.Notes.Add($"{who}: cruzeiro no máximo {Shown(cruise)}, dobra mais baixa " +
                             $"{Shown(lowestWarp)} ({cruise / lowestWarp:P0} do caminho)");
        }

        // O teto do poder condicional dela, se for um de reforço por abate.
        float reinforcement = 0f;
        var conditional = ShipEvolution.Conditional(ship, max) as StructuralReinforcementConditional;
        if (conditional != null)
            reinforcement = conditional.maxPercent;

        float defense = ShipEvolution.Value(ship, ShipStat.DefensePercent, max) + reinforcement;
        report.Warn(defense < 99f, $"{who}: defesa no pior caso chega a {defense:0}% e bate no teto de 99%");

        float extraAttackSpeed = 0f;
        foreach (string guid in AssetDatabase.FindAssets("t:AttackSpeedBoost"))
        {
            var boost = AssetDatabase.LoadAssetAtPath<AttackSpeedBoost>(AssetDatabase.GUIDToAssetPath(guid));
            if (boost != null)
                extraAttackSpeed += boost.extraAttackSpeed;
        }

        float attackSpeed = ShipEvolution.Value(ship, ShipStat.AttackSpeed, max) +
                            ship.AttackSpeed * (reinforcement / 100f + extraAttackSpeed);

        report.Notes.Add($"{who}: defesa {Math.Min(defense, 99f):0}%, cadência {attackSpeed:0.##}/s, " +
                         $"dano {ShipEvolution.Value(ship, ShipStat.Damage, max) + ship.Damage * reinforcement / 100f:0.#}, " +
                         $"vida {ShipEvolution.Value(ship, ShipStat.MaxHealth, max):0}");
    }

    /// <summary>A dobra mais fácil de alcançar, entre todas as fases e dificuldades.</summary>
    static float LowestWarpSpeed()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelSetup.CatalogPath);
        if (catalog == null)
            return 0f;

        float lowest = float.MaxValue;
        foreach (var level in catalog.levels)
        {
            if (level == null || level.endless || level.warpChargeSeconds <= 0f)
                continue;

            foreach (var settings in catalog.difficulties)
                lowest = Mathf.Min(lowest, level.warpSpeed + settings.warpSpeedBonus);
        }

        return lowest < float.MaxValue ? lowest : 0f;
    }

    static string Shown(float speed) => $"{speed * RaceSpeed.DisplayScale:0} un/s";

    // ── Fichas ───────────────────────────────────────────────────────────

    /// <summary>
    /// A versão do poder de cada patamar é da mesma classe da anterior — senão
    /// não é upgrade, é outro poder, e a linha de "próximo patamar" não tem o
    /// que comparar.
    /// </summary>
    static void CheckTiers(Report report, ShipDefinition ship)
    {
        SameKind(report, ship, ship.intrinsicAbility, ship.abilityUpgrade1, "ativo do upgrade 1");
        SameKind(report, ship, ship.abilityUpgrade1 != null ? ship.abilityUpgrade1 : ship.intrinsicAbility,
                 ship.abilityUpgrade2, "ativo do upgrade 2");
        SameKind(report, ship, ship.conditionalAbility, ship.conditionalUpgrade1, "condicional do upgrade 1");
        SameKind(report, ship, ship.conditionalUpgrade1 != null ? ship.conditionalUpgrade1 : ship.conditionalAbility,
                 ship.conditionalUpgrade2, "condicional do upgrade 2");
    }

    static void SameKind(Report report, ShipDefinition ship, ScriptableObject before, ScriptableObject after,
                         string what)
    {
        if (before == null || after == null)
            return;

        report.Warn(before.GetType() == after.GetType(),
                    $"{ship.displayName}: o {what} ({after.GetType().Name}) é de outra classe que o poder " +
                    $"anterior ({before.GetType().Name}) — é outro poder, e não um upgrade dele");
    }

    /// <summary>
    /// Todo número de poder tem nome para a tela — senão a linha do próximo
    /// patamar mostra o nome do código. Poder novo com campo novo cai aqui.
    /// </summary>
    static void CheckPowerFieldNames(Report report)
    {
        var missing = new List<string>();

        foreach (var type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
        {
            if (type.IsAbstract || !(typeof(ShipAbility).IsAssignableFrom(type) ||
                                     typeof(ShipConditional).IsAssignableFrom(type)))
                continue;

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var fieldType = field.FieldType;
                if (!fieldType.IsPrimitive && !fieldType.IsEnum && fieldType != typeof(string))
                    continue;

                if (!ShipEvolution.HasFieldName(field.Name))
                    missing.Add($"{type.Name}.{field.Name}");
            }
        }

        report.Warn(missing.Count == 0,
                    "campos de poder sem nome para a tela — entram em ShipEvolution.FieldNames: " +
                    string.Join(", ", missing));
    }

    /// <summary>Fase sem buraco: todo obstáculo e modificador ligado existe.</summary>
    static void CheckLevels(Report report)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition"))
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (level == null)
                continue;

            bool obstacles = level.obstacles != null && level.obstacles.Length > 0;
            foreach (var schedule in level.obstacles ?? Array.Empty<ObstacleSchedule>())
                obstacles &= schedule != null && schedule.stats != null;

            report.Check(obstacles, $"{level.name}: tem obstáculos, e nenhum vazio");

            bool modifiers = true;
            foreach (var modifier in level.modifiers ?? Array.Empty<LevelModifier>())
                modifiers &= modifier != null;

            report.Check(modifiers, $"{level.name}: nenhum modificador vazio no repertório");

            report.Check(level.laneCount == 0 || level.laneCount >= 2,
                         $"{level.name}: largura da pista é 0 (a da cena) ou pelo menos 2 faixas");
        }
    }

    /// <summary>O salvamento vai e volta pelo JSON sem perder nada.</summary>
    static void CheckSaveRoundTrip(Report report)
    {
        var original = new SaveData
        {
            stagesCleared = 4,
            ship = "Nave-Teste",
            wallet = 123,
            lastName = "Piloto",
        };
        original.shipLevels.Add(new ShipLevelEntry { ship = "Nave-Teste", level = 37 });
        original.records.Add(new ScoreBoard.Entry { name = "Piloto", distance = 480.5f });

        var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(original));

        report.Check(back.stagesCleared == 4 && back.ship == "Nave-Teste" && back.wallet == 123 &&
                     back.lastName == "Piloto" && back.shipLevels.Count == 1 && back.shipLevels[0].level == 37 &&
                     back.records.Count == 1 && Mathf.Approximately(back.records[0].distance, 480.5f),
                     "salvamento vai e volta pelo JSON sem perder campo");
    }
}
