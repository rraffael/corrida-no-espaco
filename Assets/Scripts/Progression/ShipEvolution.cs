using UnityEngine;

/// <summary>
/// A conta da evolução: quanto uma nave ganha num nível. É o único lugar que
/// sabe juntar a ficha da nave com o molde — o <see cref="ShipStats"/> lê daqui
/// na corrida, e a aba de naves lê daqui para mostrar os números.
///
/// **A evolução é um bônus, e não muda a base da nave** *(regra do Raffael,
/// 26/09/2026)*. Todo poder — de fase ou de nave — mede o bônus dele contra o
/// valor de fábrica da ficha. Se a evolução mexesse na base, um +40% de poder
/// passaria a render mais numa nave evoluída, e uma coisa aumentaria a outra.
/// Como bônus, cada uma soma só o que é dela.
/// </summary>
public static class ShipEvolution
{
    public static ShipEvolutionModel Model(ShipDefinition ship) =>
        ship != null && ship.evolutionModel != null ? ship.evolutionModel : ShipEvolutionModel.Default;

    public static int MaxLevel(ShipDefinition ship) => Model(ship).maxLevel;

    public static int Tier(ShipDefinition ship, int level) => Model(ship).TierAt(level);

    /// <summary>
    /// Quanto do caminho até o valor final um atributo já andou, de 0 a 1:
    /// degraus dele já subidos sobre todos os degraus dele na escadinha.
    /// </summary>
    public static float Progress(ShipEvolutionModel model, ShipStat stat, int level)
    {
        model.Ladder(out var levels, out var stats);

        int total = 0;
        int taken = 0;
        for (int i = 0; i < stats.Length; i++)
        {
            if (stats[i] != stat)
                continue;

            total++;
            if (levels[i] <= level)
                taken++;
        }

        return total > 0 ? (float)taken / total : 0f;
    }

    /// <summary>
    /// O **bônus de evolução** de um atributo naquele nível: quanto a evolução
    /// soma por cima do valor de fábrica. É o que o <see cref="ShipStats"/> aplica.
    ///
    /// O do abate desconta o que já vem da aceleração: o ganho por abate sai da
    /// aceleração de agora, então o bônus de aceleração já o faz subir — somar o
    /// ganho inteiro de novo o contaria duas vezes.
    /// </summary>
    public static float Bonus(ShipDefinition ship, ShipStat stat, int level)
    {
        if (ship == null)
            return 0f;

        if (stat == ShipStat.KillSpeedGain)
            return Value(ship, stat, level) - Value(ship, ShipStat.Acceleration, level) * ship.KillGainFactor;

        return Value(ship, stat, level) - Value(ship, stat, 1);
    }

    /// <summary>
    /// O valor de um atributo naquele nível, com o bônus de evolução e sem
    /// nenhum poder por cima. É o que a aba de naves mostra.
    /// </summary>
    public static float Value(ShipDefinition ship, ShipStat stat, int level)
    {
        var model = Model(ship);

        switch (stat)
        {
            case ShipStat.CruiseSpeed:
                return ship.CruiseSpeed * Grow(model, stat, level, model.cruiseSpeedMultiplier);
            case ShipStat.Acceleration:
                return ship.Acceleration * Grow(model, stat, level, model.accelerationMultiplier);
            case ShipStat.Damage:
                return ship.Damage * Grow(model, stat, level, model.damageMultiplier);
            case ShipStat.AttackSpeed:
                return ship.AttackSpeed * Grow(model, stat, level, model.attackSpeedMultiplier);
            case ShipStat.MaxHealth:
                return ship.MaxHealth * Grow(model, stat, level, model.maxHealthMultiplier);
            case ShipStat.DefensePercent:
                return ship.DefensePercent + model.defenseBonusPoints * Progress(model, stat, level);
            case ShipStat.KillSpeedGain:
                return Value(ship, ShipStat.Acceleration, level) * KillGainFactor(ship, level);
            case ShipStat.CrashCost:
                return ship.CrashCost;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// A fração da aceleração que vira velocidade por abate, naquele nível.
    ///
    /// **O multiplicador do molde é o do ganho TOTAL** *(decisão do Raffael,
    /// 26/09/2026)*. O ganho já sobe com a aceleração, então aqui entra só o que
    /// falta: com aceleração 1,25x e total 1,5x, o fator sobe 1,2x. Sem isso as
    /// duas subidas se multiplicariam e o abate chegaria a 1,875x.
    /// </summary>
    public static float KillGainFactor(ShipDefinition ship, int level)
    {
        var model = Model(ship);
        float own = model.killSpeedGainMultiplier / Mathf.Max(0.01f, model.accelerationMultiplier);
        return ship.KillGainFactor * Grow(model, ShipStat.KillSpeedGain, level, own);
    }

    static float Grow(ShipEvolutionModel model, ShipStat stat, int level, float finalMultiplier) =>
        Mathf.Lerp(1f, finalMultiplier, Progress(model, stat, level));

    // ── Patamares ───────────────────────────────────────────────────────

    /// <summary>
    /// O poder ativo naquele nível: a versão do patamar mais alto alcançado que
    /// tenha uma. Patamar vazio mantém a anterior — nave sem upgrade desenhado
    /// ainda funciona, só não muda.
    /// </summary>
    public static ShipAbility Ability(ShipDefinition ship, int level)
    {
        if (ship == null)
            return null;

        int tier = Tier(ship, level);
        if (tier >= 3 && ship.abilityUpgrade2 != null) return ship.abilityUpgrade2;
        if (tier >= 2 && ship.abilityUpgrade1 != null) return ship.abilityUpgrade1;
        return ship.intrinsicAbility;
    }

    /// <summary>O poder condicional naquele nível. Mesma regra do ativo.</summary>
    public static ShipConditional Conditional(ShipDefinition ship, int level)
    {
        if (ship == null)
            return null;

        int tier = Tier(ship, level);
        if (tier >= 3 && ship.conditionalUpgrade2 != null) return ship.conditionalUpgrade2;
        if (tier >= 2 && ship.conditionalUpgrade1 != null) return ship.conditionalUpgrade1;
        return ship.conditionalAbility;
    }

    /// <summary>A passiva, só no patamar final.</summary>
    public static ShipPassive Passive(ShipDefinition ship, int level) =>
        ship != null && Tier(ship, level) >= 4 ? ship.passiveAbility : null;

    // ── Para a tela ─────────────────────────────────────────────────────

    /// <summary>
    /// O próximo patamar que a nave ainda não alcançou, e o que ele dá: "Nível
    /// 15 · Tiros teleguiados: duração 3 → 4 · recarga 30 → 25". Vazio quando
    /// todos já foram alcançados.
    ///
    /// Existe para o jogador — e o Raffael testando — **ver o que o upgrade dá
    /// antes de pegar**, e não só depois.
    /// </summary>
    public static string DescribeNextTier(ShipDefinition ship, int level)
    {
        var model = Model(ship);
        int tier = model.TierAt(level);

        switch (tier)
        {
            case 1: return $"Nível {model.upgrade1Level} · {DescribeUpgrade(ship, 1, 2)}";
            case 2: return $"Nível {model.upgrade2Level} · {DescribeUpgrade(ship, 2, 3)}";
            case 3: return $"Nível {model.passiveLevel} · {DescribePassive(ship)}";
            default: return string.Empty;
        }
    }

    static string DescribeUpgrade(ShipDefinition ship, int fromTier, int toTier)
    {
        var model = Model(ship);
        int fromLevel = LevelOf(model, fromTier);
        int toLevel = LevelOf(model, toTier);

        string active = Difference(Ability(ship, fromLevel), Ability(ship, toLevel));
        string triggered = Difference(Conditional(ship, fromLevel), Conditional(ship, toLevel));

        if (active == null && triggered == null)
            return "upgrade do poder a definir";

        if (active != null && triggered != null)
            return active + "  ·  " + triggered;

        return active ?? triggered;
    }

    static string DescribePassive(ShipDefinition ship)
    {
        var passive = ship.passiveAbility;
        if (passive == null)
            return "passiva a definir";

        return string.IsNullOrEmpty(passive.description)
            ? $"passiva {passive.displayName}"
            : $"passiva {passive.displayName} — {passive.description}";
    }

    static int LevelOf(ShipEvolutionModel model, int tier)
    {
        switch (tier)
        {
            case 2: return model.upgrade1Level;
            case 3: return model.upgrade2Level;
            case 4: return model.passiveLevel;
            default: return 1;
        }
    }

    /// <summary>
    /// O que muda de uma ficha de poder para a outra, campo por campo: "Tiros
    /// teleguiados: duração (s) 3 → 4". <c>null</c> quando não há upgrade — a
    /// mesma ficha nos dois patamares, ou nenhuma.
    ///
    /// **Lê os campos sozinho**, então poder novo com número novo aparece aqui sem
    /// ninguém mexer nesta classe. Só o nome bonito do campo é que precisa entrar
    /// em <see cref="FieldNames"/>; sem ele, sai o nome do código.
    /// </summary>
    static string Difference(ScriptableObject from, ScriptableObject to)
    {
        if (to == null || from == to)
            return null;

        if (from == null || from.GetType() != to.GetType())
            return $"ganha {DisplayName(to)}";

        var changes = new System.Collections.Generic.List<string>();
        foreach (var field in to.GetType().GetFields(System.Reflection.BindingFlags.Public |
                                                     System.Reflection.BindingFlags.Instance))
        {
            if (Hidden(field.Name))
                continue;

            var type = field.FieldType;
            if (!type.IsPrimitive && !type.IsEnum && type != typeof(string))
                continue;

            object before = field.GetValue(from);
            object after = field.GetValue(to);
            if (Equals(before, after))
                continue;

            changes.Add($"{FieldName(field.Name)} {Show(field.Name, before)} → {Show(field.Name, after)}");
        }

        string name = DisplayName(from);
        return changes.Count == 0
            ? $"{name}: mesma ficha, sem mudança nos números"
            : $"{name}: {string.Join(" · ", changes)}";
    }

    static string DisplayName(ScriptableObject power)
    {
        switch (power)
        {
            case ShipAbility ability: return ability.displayName;
            case ShipConditional conditional: return conditional.displayName;
            default: return power.name;
        }
    }

    /// <summary>Texto e identidade não são upgrade — só o nome, que aparece se mudar.</summary>
    static bool Hidden(string field) =>
        field == "description" || field == "trigger";

    /// <summary>
    /// O nome de cada campo de poder como o jogador lê. **Campo novo de poder
    /// novo entra aqui** para sair bonito; sem entrar, aparece o nome do código.
    /// </summary>
    static readonly System.Collections.Generic.Dictionary<string, string> FieldNames =
        new System.Collections.Generic.Dictionary<string, string>
        {
            { "displayName", "nome" },
            { "usesPerRace", "usos por corrida" },
            { "cooldownSeconds", "recarga (s)" },
            { "lifetime", "tipo de duração" },
            { "durationSeconds", "duração (s)" },
            { "charges", "cargas" },
            { "percentPerKill", "% por carga" },
            { "maxPercent", "teto (%)" },
        };

    static string FieldName(string field) =>
        FieldNames.TryGetValue(field, out var shown) ? shown : field;

    /// <summary>
    /// Este campo de poder tem nome para a tela? As verificações do Editor
    /// perguntam aqui para avisar de campo novo que ficou sem.
    /// </summary>
    public static bool HasFieldName(string field) => FieldNames.ContainsKey(field) || Hidden(field);

    static string Show(string field, object value)
    {
        // Usos em zero querem dizer "sem limite", e "0 → 2" leria como ganho.
        if (field == "usesPerRace" && value is int uses && uses <= 0)
            return "sem limite";

        return value is float number ? number.ToString("0.##") : value.ToString();
    }

    /// <summary>
    /// O que chegar a um nível dá, numa linha: "+6,3 de vida", ou o patamar.
    /// Mostra o número que sobe de verdade — o do abate em un/s, e não um
    /// "1,5x" que o jogador não vê em lugar nenhum.
    /// </summary>
    public static string DescribeLevel(ShipDefinition ship, int level)
    {
        var model = Model(ship);

        // Patamar diz só qual é: o que ele dá mora na linha do próximo patamar,
        // que aparece desde antes de chegar nele.
        switch (model.TierOpenedAt(level))
        {
            case 2: return "1º upgrade do poder (ver abaixo)";
            case 3: return "2º upgrade do poder (ver abaixo)";
            case 4: return "passiva (ver abaixo)";
        }

        model.Ladder(out var levels, out var stats);
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != level)
                continue;

            var stat = stats[i];
            float gain = Value(ship, stat, level) - Value(ship, stat, level - 1);
            return $"+{Format(stat, gain)} {Name(stat)}";
        }

        return string.Empty;
    }

    /// <summary>Nome do atributo como o jogador lê.</summary>
    public static string Name(ShipStat stat)
    {
        switch (stat)
        {
            case ShipStat.CruiseSpeed: return "cruzeiro";
            case ShipStat.Acceleration: return "aceleração";
            case ShipStat.Damage: return "dano";
            case ShipStat.AttackSpeed: return "cadência";
            case ShipStat.MaxHealth: return "vida";
            case ShipStat.DefensePercent: return "defesa";
            case ShipStat.KillSpeedGain: return "por abate";
            default: return "custo da batida";
        }
    }

    /// <summary>
    /// O número na unidade da tela. Velocidade, aceleração e ganho por abate
    /// saem na escala do jogo (<see cref="RaceSpeed.DisplayScale"/>), para
    /// baterem com o que o painel da corrida mostra.
    /// </summary>
    public static string Format(ShipStat stat, float value)
    {
        switch (stat)
        {
            case ShipStat.CruiseSpeed:
            case ShipStat.KillSpeedGain:
                return $"{value * RaceSpeed.DisplayScale:0.#} un/s";
            case ShipStat.Acceleration:
                return $"{value * RaceSpeed.DisplayScale:0.#} un/s²";
            case ShipStat.DefensePercent:
                return $"{value:0.#}%";
            case ShipStat.AttackSpeed:
                return $"{value:0.##}/s";
            default:
                return $"{value:0.#}";
        }
    }
}
