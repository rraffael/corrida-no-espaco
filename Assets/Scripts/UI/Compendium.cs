using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// O **Compêndio** *(pedido do Raffael, 18/10/2026)*: tudo o que existe no jogo,
/// num modelo só — imagem, descrição, atributos, poder e extra. Esta classe
/// transforma cada ficha num <see cref="Entry"/>; quem desenha é o
/// <see cref="CompendiumMenu"/>.
///
/// **Quase tudo é calculado das fichas**, e não escrito à parte: atributo,
/// usos, patamares, em que fases aparece. Assim o Compêndio não mente quando
/// um número muda. O que não sai de conta — a descrição e as curiosidades —
/// mora na própria ficha (<c>description</c> e <c>extraNotes</c>).
///
/// Seção vazia não aparece: um modificador não tem "Poder", e mostrar o título
/// sem nada embaixo faria parecer que falta algo.
/// </summary>
public static class Compendium
{
    public enum Section
    {
        Ships,
        Modifiers,
        Obstacles,
    }

    /// <summary>Uma coisa do jogo, já em texto para a tela.</summary>
    public class Entry
    {
        public string Title;
        public string Subtitle;
        public Sprite Image;
        public Color Tint = Color.white;
        public string Description;
        public string Attributes;
        public string Power;
        public string Extra;
    }

    public static List<Entry> Entries(Section section)
    {
        var list = new List<Entry>();
        var catalog = CompendiumCatalog.Load();

        switch (section)
        {
            case Section.Ships:
                var ships = ShipCatalog.Load();
                if (ships != null)
                {
                    foreach (var ship in ships.ships)
                    {
                        if (ship != null)
                            list.Add(FromShip(ship, catalog));
                    }
                }

                break;

            case Section.Modifiers:
                if (catalog != null)
                {
                    foreach (var modifier in catalog.modifiers)
                    {
                        if (modifier != null)
                            list.Add(FromModifier(modifier, catalog));
                    }
                }

                break;

            case Section.Obstacles:
                if (catalog != null)
                {
                    foreach (var obstacle in catalog.obstacles)
                    {
                        if (obstacle != null)
                            list.Add(FromObstacle(obstacle));
                    }
                }

                break;
        }

        return list;
    }

    // ── Naves ────────────────────────────────────────────────────────────

    static Entry FromShip(ShipDefinition ship, CompendiumCatalog catalog)
    {
        var model = ShipEvolution.Model(ship);
        int max = ShipEvolution.MaxLevel(ship);

        return new Entry
        {
            Title = ship.displayName,
            Subtitle = "Nave",
            Image = catalog != null ? catalog.placeholder : null,
            Tint = ship.color,
            Description = ship.description,
            Attributes = $"<b>Nível 1</b>\n{StatsLine(ship, 1)}\n<b>Nível {max}</b>\n{StatsLine(ship, max)}",
            Power = ShipPowers(ship, model),
            Extra = Join(ship.extraNotes,
                         $"Patamares nos níveis {model.upgrade1Level}, {model.upgrade2Level} e {model.passiveLevel}: " +
                         "os dois primeiros melhoram o poder, o último liga a passiva."),
        };
    }

    static string StatsLine(ShipDefinition ship, int level)
    {
        string V(ShipStat stat) => ShipEvolution.Format(stat, ShipEvolution.Value(ship, stat, level));

        return $"Vida {V(ShipStat.MaxHealth)} · Defesa {V(ShipStat.DefensePercent)} · " +
               $"Dano {V(ShipStat.Damage)} · Cadência {V(ShipStat.AttackSpeed)}\n" +
               $"Cruzeiro {V(ShipStat.CruiseSpeed)} · Aceleração {V(ShipStat.Acceleration)} · " +
               $"Abate +{V(ShipStat.KillSpeedGain)}";
    }

    static string ShipPowers(ShipDefinition ship, ShipEvolutionModel model)
    {
        var text = new StringBuilder();

        var ability = ship.intrinsicAbility;
        if (ability != null)
        {
            Line(text, $"<b>Ativo — {ability.displayName}</b> (toque duplo)");
            Line(text, ability.description);
            Line(text, ShipSelectRow.Activation(ability));
        }

        var conditional = ship.conditionalAbility;
        if (conditional != null)
        {
            Line(text, $"<b>Condicional — {conditional.displayName}</b>");
            Line(text, conditional.description);
            if (!string.IsNullOrEmpty(conditional.trigger))
                Line(text, "Dispara " + conditional.trigger);
        }

        // Os upgrades, na mesma conta que a aba de naves usa para o "próximo
        // patamar": a ficha de cada patamar contra a do anterior.
        if (ability != null || conditional != null)
        {
            Line(text, Tier(ship, 1));
            Line(text, Tier(ship, model.upgrade1Level));
        }

        var passive = ship.passiveAbility;
        if (passive != null)
        {
            Line(text, $"<b>Passiva — {passive.displayName}</b> (nível {model.passiveLevel})");
            Line(text, passive.description);
        }
        else if (ability != null || conditional != null)
        {
            Line(text, $"<b>Passiva</b> — a definir (nível {model.passiveLevel})");
        }

        return text.ToString();
    }

    static string Tier(ShipDefinition ship, int fromLevel)
    {
        string tier = ShipEvolution.DescribeNextTier(ship, fromLevel);
        return string.IsNullOrEmpty(tier) ? null : "Upgrade · " + tier;
    }

    // ── Modificadores de fase ────────────────────────────────────────────

    static Entry FromModifier(LevelModifier modifier, CompendiumCatalog catalog)
    {
        string category = LevelModifier.NameFor(modifier.category);

        return new Entry
        {
            Title = modifier.displayName,
            Subtitle = "Modificador de fase · " + category,
            Image = modifier.sprite != null ? modifier.sprite : catalog != null ? catalog.placeholder : null,
            Tint = modifier.Color,
            Description = modifier.description,
            Attributes = $"Categoria {category} · {Duration(modifier)}",
            Extra = Join(modifier.extraNotes,
                         CategoryHint(modifier.category),
                         Appearances(level => level.modifiers != null &&
                                              System.Array.IndexOf(level.modifiers, modifier) >= 0)),
        };
    }

    static string Duration(LevelModifier modifier)
    {
        switch (modifier.lifetime)
        {
            case LevelModifier.Lifetime.Instantaneo: return "instantâneo";
            case LevelModifier.Lifetime.PorUso: return modifier.charges == 1 ? "1 carga" : $"{modifier.charges} cargas";
            default: return $"dura {modifier.durationSeconds:0.#} s";
        }
    }

    static string CategoryHint(LevelModifier.Category category)
    {
        switch (category)
        {
            case LevelModifier.Category.Debilitante: return "Vermelho na pista: desviar é bom.";
            case LevelModifier.Category.Especial: return "Roxo na pista: não é simplesmente bom nem ruim.";
            default: return "Verde na pista: pegar é bom.";
        }
    }

    // ── Obstáculos e inimigos ────────────────────────────────────────────

    static Entry FromObstacle(ObstacleStats obstacle)
    {
        return new Entry
        {
            Title = obstacle.displayName,
            Subtitle = "Obstáculo",
            Image = obstacle.sprite,
            Tint = obstacle.color,
            Description = obstacle.description,
            Attributes = $"Vida {obstacle.maxHealth:0} · Dano da batida {obstacle.contactDamage:0} · " +
                         $"Ocupa {Lanes(obstacle.laneSpan)}\n" +
                         $"Peso do abate {obstacle.killWeight:0.#} · Freada na batida {obstacle.speedPenaltyOnCrash:0.#}",
            Power = ObstaclePowers(obstacle),
            Extra = Join(obstacle.extraNotes,
                         DifficultyNote(),
                         Appearances(level => Has(level, obstacle))),
        };
    }

    static string Lanes(int span) => span <= 1 ? "1 faixa" : $"{span} faixas";

    static string ObstaclePowers(ObstacleStats obstacle)
    {
        var text = new StringBuilder();

        if (obstacle.shrapnelOnDeath)
        {
            string pieces = obstacle.shrapnelCount == 1 ? "1 estilhaço" : $"{obstacle.shrapnelCount} estilhaços";
            Line(text, "<b>Estilhaços</b>");
            Line(text, $"Destruído, solta {pieces} de {obstacle.shrapnelDamage:0} de dano, que descem pela " +
                       "faixa dele mais rápido que a pista. Quem atira parado embaixo leva.");
        }

        if (obstacle.pullsOnPass)
        {
            Line(text, "<b>Vácuo</b>");
            Line(text, "Se passar pela faixa vizinha da nave sem ser destruído, puxa a nave uma faixa na " +
                       "direção dele quando some da tela. Destruído a tiro, não puxa.");
        }

        return text.ToString();
    }

    static bool Has(LevelDefinition level, ObstacleStats obstacle)
    {
        if (level.obstacles == null)
            return false;

        foreach (var schedule in level.obstacles)
        {
            if (schedule != null && schedule.stats == obstacle)
                return true;
        }

        return false;
    }

    /// <summary>"No Normal, vida ×1,3 e dano ×1,2 · no Difícil, …" — lido do catálogo de fases.</summary>
    static string DifficultyNote()
    {
        var levels = LevelCatalog.Load();
        if (levels == null || levels.difficulties == null)
            return null;

        var parts = new List<string>();
        foreach (var settings in levels.difficulties)
        {
            if (settings == null ||
                (Mathf.Approximately(settings.obstacleHealthFactor, 1f) &&
                 Mathf.Approximately(settings.obstacleDamageFactor, 1f)))
                continue;

            parts.Add($"no {settings.displayName}, vida ×{settings.obstacleHealthFactor:0.##} e dano " +
                      $"×{settings.obstacleDamageFactor:0.##}");
        }

        return parts.Count == 0 ? null : "Mais duro com a dificuldade: " + string.Join("; ", parts) + ".";
    }

    // ── Comum ────────────────────────────────────────────────────────────

    /// <summary>"Aparece em: Fase 2 e Sem fim", ou que não está em fase nenhuma.</summary>
    static string Appearances(System.Predicate<LevelDefinition> appearsIn)
    {
        var levels = LevelCatalog.Load();
        if (levels == null)
            return null;

        var names = new List<string>();
        foreach (var level in levels.levels)
        {
            if (level != null && appearsIn(level))
                names.Add(level.displayName);
        }

        if (levels.endlessLevel != null && appearsIn(levels.endlessLevel))
            names.Add(levels.endlessLevel.displayName);

        if (names.Count == 0)
            return "Ainda não aparece em fase nenhuma.";

        string joined = names.Count == 1
            ? names[0]
            : string.Join(", ", names.GetRange(0, names.Count - 1)) + " e " + names[names.Count - 1];
        return "Aparece em: " + joined + ".";
    }

    static void Line(StringBuilder text, string line)
    {
        if (string.IsNullOrEmpty(line))
            return;

        if (text.Length > 0)
            text.Append('\n');
        text.Append(line);
    }

    static string Join(params string[] lines)
    {
        var text = new StringBuilder();
        foreach (var line in lines)
            Line(text, line);
        return text.ToString();
    }
}
