using System.Collections.Generic;

/// <summary>
/// O **poder condicional da nave nesta corrida**. A ficha
/// (<see cref="ShipConditional"/>) é um arquivo compartilhado e não pode guardar
/// nada que mude durante a partida; quem guarda é isto aqui. Nasce na largada e
/// morre no fim, um por corrida.
/// </summary>
public class ActiveShipConditional
{
    public ShipConditional Definition { get; private set; }

    /// <summary>Os atributos da nave, para os condicionais que mexem neles.</summary>
    public ShipStats Stats { get; private set; }

    /// <summary>
    /// Quantas vezes a condição já valeu nesta corrida. É contador genérico — o
    /// que ele conta, e se tem teto, é a filha quem diz.
    /// </summary>
    public int Count { get; set; }

    List<StatModifier> applied;

    internal ActiveShipConditional(ShipConditional definition, ShipStats stats)
    {
        Definition = definition;
        Stats = stats;
    }

    /// <summary>
    /// Mexe num número da nave até o fim da corrida, ou até
    /// <see cref="RemoveModifiers"/>. Não precisa desfazer no fim.
    /// </summary>
    public void ApplyModifier(StatModifier modifier)
    {
        if (modifier == null || Stats == null)
            return;

        applied ??= new List<StatModifier>();
        applied.Add(modifier);
        Stats.AddModifier(modifier);
    }

    /// <summary>
    /// Retira tudo o que este condicional aplicou. Serve para quem cresce aos
    /// degraus: trocar o degrau inteiro por um novo é mais simples que empilhar
    /// um modificador por disparo, e deixa a lista da nave com poucos itens.
    /// </summary>
    public void RemoveModifiers()
    {
        if (applied == null || applied.Count == 0)
            return;

        if (Stats != null)
        {
            for (int i = 0; i < applied.Count; i++)
                Stats.RemoveModifier(applied[i]);
        }

        applied.Clear();
    }
}
