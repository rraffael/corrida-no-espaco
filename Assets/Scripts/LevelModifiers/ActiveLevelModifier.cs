using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Um **modificador de fase enquanto ele vale**, nesta corrida. A ficha
/// (<see cref="LevelModifier"/>) é um arquivo compartilhado e não pode guardar
/// nada que mude durante a partida; quem guarda é isto aqui.
///
/// **Um por ficha, e não um por vez que se pega** *(regra do Raffael,
/// 30/08/2026)*. Passar em cima do mesmo modificador de novo **renova** este —
/// o tempo volta ao cheio — em vez de criar um segundo. Dois modificadores
/// **diferentes** viram dois destes, cada um com o próprio relógio, correndo
/// independentes. Ver <see cref="LevelModifiers.Grant"/>.
///
/// **Não é selado de propósito:** um modificador que precise lembrar de algo que
/// só ele tem cria a própria classe filha e a devolve em
/// <see cref="LevelModifier.CreateRuntime"/>.
/// </summary>
public class ActiveLevelModifier
{
    public LevelModifier Definition { get; private set; }

    /// <summary>Os modificadores de fase da nave que carrega este.</summary>
    public LevelModifiers Owner { get; private set; }

    /// <summary>Segundos restantes. Só significa algo com duração PorTempo.</summary>
    public float SecondsLeft { get; internal set; }

    /// <summary>Cargas restantes. Só significa algo com duração PorUso.</summary>
    public int ChargesLeft { get; internal set; }

    /// <summary>Já acabou e está esperando ser recolhido.</summary>
    public bool Finished { get; private set; }

    /// <summary>
    /// Quanto ainda falta, de 1 (acabou de pegar) a 0 (acabando). É o que o
    /// quadradinho do HUD desenha esvaziando, e por isso vale para as três
    /// formas de duração: por tempo é a fração do relógio, por carga é a fração
    /// das cargas, e instantâneo nunca chega a aparecer.
    /// </summary>
    public float Fraction
    {
        get
        {
            if (Definition == null)
                return 0f;

            switch (Definition.lifetime)
            {
                case LevelModifier.Lifetime.PorTempo:
                    return Definition.durationSeconds > 0f
                        ? Mathf.Clamp01(SecondsLeft / Definition.durationSeconds)
                        : 0f;

                case LevelModifier.Lifetime.PorUso:
                    return Definition.charges > 0
                        ? Mathf.Clamp01((float)ChargesLeft / Definition.charges)
                        : 0f;

                default:
                    return 1f;
            }
        }
    }

    internal void Bind(LevelModifier definition, LevelModifiers owner)
    {
        Definition = definition;
        Owner = owner;
        Renew();
    }

    /// <summary>Devolve tempo e cargas ao cheio. É o que a repescagem faz.</summary>
    internal void Renew()
    {
        SecondsLeft = Definition.durationSeconds;
        ChargesLeft = Definition.charges;
    }

    /// <summary>
    /// Gasta uma carga. Quando a última vai embora, o modificador acaba **na
    /// hora** — não é preciso a filha lembrar de encerrar, e o golpe seguinte já
    /// não o encontra, mesmo vindo no mesmo instante.
    /// </summary>
    public void ConsumeCharge(int count = 1)
    {
        ChargesLeft -= Mathf.Max(1, count);

        if (ChargesLeft <= 0)
            Finish();
    }

    /// <summary>Encerra agora, seja qual for o tempo ou a carga que sobrava.</summary>
    public void Finish() => Finished = true;

    // ── Modificadores de atributo e traços ───────────────────────────────

    /// <summary>Criadas só quando o modificador mexe em algo, que é a minoria dos casos.</summary>
    List<StatModifier> appliedModifiers;
    List<ShipTrait> appliedTraits;

    /// <summary>
    /// Mexe num número da nave enquanto este modificador valer. **Não precisa
    /// desfazer no <c>OnLost</c>:** tudo o que foi aplicado por aqui é retirado
    /// sozinho quando ele acaba, por tempo, por carga ou por fim de corrida.
    /// </summary>
    public void ApplyModifier(StatModifier modifier)
    {
        var stats = Owner != null ? Owner.Stats : null;
        if (modifier == null || stats == null)
            return;

        appliedModifiers ??= new List<StatModifier>();
        appliedModifiers.Add(modifier);
        stats.AddModifier(modifier);
    }

    /// <summary>
    /// Liga um traço da nave enquanto este modificador valer. Traço é o que não
    /// é número — ver <see cref="ShipTrait"/>. Também se desfaz sozinho.
    /// </summary>
    public void ApplyTrait(ShipTrait trait)
    {
        var stats = Owner != null ? Owner.Stats : null;
        if (stats == null)
            return;

        appliedTraits ??= new List<ShipTrait>();
        appliedTraits.Add(trait);
        stats.AddTrait(trait);
    }

    /// <summary>Retira o que este modificador tinha aplicado. Chamado pelo sistema, no fim.</summary>
    internal void RemoveApplied()
    {
        var stats = Owner != null ? Owner.Stats : null;

        if (appliedModifiers != null && appliedModifiers.Count > 0)
        {
            if (stats != null)
            {
                for (int i = 0; i < appliedModifiers.Count; i++)
                    stats.RemoveModifier(appliedModifiers[i]);
            }

            appliedModifiers.Clear();
        }

        if (appliedTraits == null || appliedTraits.Count == 0)
            return;

        if (stats != null)
        {
            for (int i = 0; i < appliedTraits.Count; i++)
                stats.RemoveTrait(appliedTraits[i]);
        }

        appliedTraits.Clear();
    }
}
