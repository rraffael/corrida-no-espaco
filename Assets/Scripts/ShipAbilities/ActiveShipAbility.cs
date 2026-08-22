using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Um **poder da nave enquanto o efeito dele vale**, depois de ativado. A ficha
/// (<see cref="ShipAbility"/>) é um arquivo compartilhado e não pode guardar
/// nada que mude durante a partida; quem guarda é isto aqui.
///
/// Cada ativação cria um destes. Os **usos por partida** e a **recarga** não
/// moram aqui — são do slot equipado, no <see cref="ShipAbilities"/>, porque
/// existem mesmo quando nenhum efeito está valendo.
///
/// **Não é selado de propósito:** um poder que precise lembrar de algo que só
/// ele tem cria a própria classe filha e a devolve em
/// <see cref="ShipAbility.CreateRuntime"/>.
/// </summary>
public class ActiveShipAbility
{
    public ShipAbility Definition { get; private set; }

    /// <summary>O slot de poder da nave que ativou este efeito.</summary>
    public ShipAbilities Owner { get; private set; }

    /// <summary>Segundos restantes. Só significa algo com duração PorTempo.</summary>
    public float SecondsLeft { get; internal set; }

    /// <summary>Cargas restantes. Só significa algo com duração PorUso.</summary>
    public int ChargesLeft { get; internal set; }

    /// <summary>Já acabou e está esperando ser recolhido.</summary>
    public bool Finished { get; private set; }

    internal void Bind(ShipAbility definition, ShipAbilities owner)
    {
        Definition = definition;
        Owner = owner;
        SecondsLeft = definition.durationSeconds;
        ChargesLeft = definition.charges;
    }

    /// <summary>
    /// Gasta uma carga do efeito. Quando a última vai embora, o efeito acaba
    /// **na hora** — o golpe seguinte já não o encontra, mesmo vindo no mesmo
    /// instante.
    /// </summary>
    public void ConsumeCharge(int count = 1)
    {
        ChargesLeft -= Mathf.Max(1, count);

        if (ChargesLeft <= 0)
            Finish();
    }

    /// <summary>Encerra o efeito agora, seja qual for o tempo ou a carga que sobrava.</summary>
    public void Finish() => Finished = true;

    // ── Modificadores de atributo ────────────────────────────────────────

    /// <summary>Criada só quando o poder mexe em atributo.</summary>
    List<StatModifier> applied;

    /// <summary>
    /// Mexe num número da nave enquanto este efeito valer. **Não precisa
    /// desfazer no <c>OnEnded</c>:** tudo o que foi aplicado por aqui é retirado
    /// sozinho quando o efeito acaba.
    /// </summary>
    public void ApplyModifier(StatModifier modifier)
    {
        var stats = Owner != null ? Owner.Stats : null;
        if (modifier == null || stats == null)
            return;

        applied ??= new List<StatModifier>();
        applied.Add(modifier);
        stats.AddModifier(modifier);
    }

    /// <summary>Retira o que este efeito tinha aplicado. Chamado pelo sistema, no fim.</summary>
    internal void RemoveAppliedModifiers()
    {
        if (applied == null || applied.Count == 0)
            return;

        var stats = Owner != null ? Owner.Stats : null;
        if (stats != null)
        {
            for (int i = 0; i < applied.Count; i++)
                stats.RemoveModifier(applied[i]);
        }

        applied.Clear();
    }
}
