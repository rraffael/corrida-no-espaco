using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Um poder **enquanto ele vale**, nesta corrida. A ficha
/// (<see cref="PowerUpDefinition"/>) é um arquivo compartilhado e não pode
/// guardar nada que mude durante a partida; quem guarda é isto aqui.
///
/// Um poder pego duas vezes vira dois destes, cada um com o próprio tempo e as
/// próprias cargas.
///
/// **Não é selado de propósito:** um poder que precise lembrar de algo que só ele
/// tem — o valor original de um atributo, quantos inimigos já contou — cria a
/// própria classe filha e a devolve em
/// <see cref="PowerUpDefinition.CreateRuntime"/>.
/// </summary>
public class ActivePowerUp
{
    public PowerUpDefinition Definition { get; private set; }

    /// <summary>A nave que carrega este poder.</summary>
    public ShipPowerUps Ship { get; private set; }

    /// <summary>Segundos restantes. Só significa algo com duração PorTempo.</summary>
    public float SecondsLeft { get; internal set; }

    /// <summary>Cargas restantes. Só significa algo com duração PorUso.</summary>
    public int ChargesLeft { get; internal set; }

    /// <summary>Já acabou e está esperando ser recolhido no fim do quadro.</summary>
    public bool Finished { get; private set; }

    internal void Bind(PowerUpDefinition definition, ShipPowerUps ship)
    {
        Definition = definition;
        Ship = ship;
        SecondsLeft = definition.durationSeconds;
        ChargesLeft = definition.charges;
    }

    /// <summary>
    /// Gasta uma carga. Quando a última vai embora, o poder acaba — não é preciso
    /// a filha lembrar de encerrar.
    /// </summary>
    public void ConsumeCharge(int count = 1)
    {
        ChargesLeft -= Mathf.Max(1, count);

        if (ChargesLeft <= 0)
            Finish();
    }

    /// <summary>Encerra o poder agora, seja qual for o tempo ou a carga que sobrava.</summary>
    public void Finish() => Finished = true;

    // ── Modificadores de atributo ────────────────────────────────────────

    /// <summary>Criada só quando o poder mexe em atributo, que é a minoria dos casos.</summary>
    List<StatModifier> applied;

    /// <summary>
    /// Mexe num número da nave enquanto este poder valer. **Não precisa desfazer
    /// no <c>OnLost</c>:** tudo o que foi aplicado por aqui é retirado sozinho
    /// quando o poder acaba, por tempo, por carga ou por fim de corrida.
    ///
    /// É o caminho de qualquer poder do tipo "fica melhor em X por um tempo" —
    /// tiro rápido é <c>ApplyModifier(StatModifier.Times(ShipStat.AttackSpeed, 2f))</c>
    /// no <c>OnGained</c>, e mais nada.
    /// </summary>
    public void ApplyModifier(StatModifier modifier)
    {
        var stats = Ship != null ? Ship.Stats : null;
        if (modifier == null || stats == null)
            return;

        applied ??= new List<StatModifier>();
        applied.Add(modifier);
        stats.AddModifier(modifier);
    }

    /// <summary>Retira o que este poder tinha aplicado. Chamado pelo sistema, no fim.</summary>
    internal void RemoveAppliedModifiers()
    {
        if (applied == null || applied.Count == 0)
            return;

        var stats = Ship != null ? Ship.Stats : null;
        if (stats != null)
        {
            for (int i = 0; i < applied.Count; i++)
                stats.RemoveModifier(applied[i]);
        }

        applied.Clear();
    }
}
