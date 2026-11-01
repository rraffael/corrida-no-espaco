using System.Collections.Generic;

/// <summary>
/// A **passiva da nave nesta corrida**. A ficha (<see cref="ShipPassive"/>) é um
/// arquivo compartilhado e não pode guardar nada que mude durante a partida; quem
/// guarda é isto aqui. Nasce na largada e morre no fim, um por corrida.
/// </summary>
public class ActiveShipPassive
{
    public ShipPassive Definition { get; private set; }

    /// <summary>O slot de poder da nave, para a passiva que ativa o poder sozinha.</summary>
    public ShipAbilities Owner { get; private set; }

    public ShipStats Stats => Owner != null ? Owner.Stats : null;

    /// <summary>Contador genérico: o que ele conta, e se tem teto, é a filha quem diz.</summary>
    public int Count { get; set; }

    /// <summary>Relógio genérico, para a passiva que tem janela de tempo.</summary>
    public float SecondsLeft { get; set; }

    List<StatModifier> applied;

    internal ActiveShipPassive(ShipPassive definition, ShipAbilities owner)
    {
        Definition = definition;
        Owner = owner;
    }

    /// <summary>
    /// Mexe num número da nave até o fim da corrida, ou até
    /// <see cref="RemoveModifiers"/>. Não precisa desfazer no fim.
    /// </summary>
    public void ApplyModifier(StatModifier modifier)
    {
        var stats = Stats;
        if (modifier == null || stats == null)
            return;

        applied ??= new List<StatModifier>();
        applied.Add(modifier);
        stats.AddModifier(modifier);
    }

    /// <summary>Retira tudo o que esta passiva aplicou.</summary>
    public void RemoveModifiers()
    {
        if (applied == null || applied.Count == 0)
            return;

        var stats = Stats;
        if (stats != null)
        {
            for (int i = 0; i < applied.Count; i++)
                stats.RemoveModifier(applied[i]);
        }

        applied.Clear();
    }
}
