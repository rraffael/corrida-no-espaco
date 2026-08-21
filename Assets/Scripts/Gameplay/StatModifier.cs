using UnityEngine;

/// <summary>
/// Um número da nave que pode ser mexido de fora. São os atributos da ficha que
/// alguma coisa lê durante a corrida — e por isso são exatamente os que um poder
/// ou uma evolução podem querer alterar.
/// </summary>
public enum ShipStat
{
    CruiseSpeed,
    Acceleration,
    Damage,
    AttackSpeed,
    MaxHealth,
    DefensePercent,

    /// <summary>
    /// Velocidade que um abate de peso 1 rende. É derivado da aceleração, então
    /// mexer na aceleração já mexe aqui — este modificador é para quando se
    /// quiser mexer **só** no ganho por abate.
    /// </summary>
    KillSpeedGain,

    /// <summary>
    /// Fator do preço de uma batida: o atraso, o trecho arrastado e a punição de
    /// velocidade, todos multiplicados por ele. Não mexe na profundidade da
    /// queda — o susto é o mesmo, o que muda é quanto tempo se perde voltando.
    /// </summary>
    CrashCost,
}

/// <summary>
/// Uma alteração temporária num número da nave. É por aqui que um poder faz
/// "cadência dobrada por 8 segundos" sem nunca encostar na ficha — que é um
/// arquivo compartilhado e não pode guardar nada de uma partida.
///
/// **Todo modificador mede o próprio bônus contra o valor de base, e os bônus se
/// somam** *(regra do Raffael, 21/08/2026)*. Com aceleração 4, um <c>+2</c> e um
/// <c>×1,5</c> valem 2 e 2 — os dois medidos contra o 4 —, e o total é 8.
///
/// É o que garante que **a ordem nunca importa** e que **empilhar não estoura**:
/// dois <c>×1,5</c> dão o dobro da base, não 2,25 vezes. Ver
/// <see cref="ShipStats"/> para a conta.
///
/// **É imutável de propósito.** Quem aplicou guarda a referência e devolve a
/// mesma para remover; um modificador que mudasse de valor depois de aplicado
/// deixaria o cache do <see cref="ShipStats"/> mentindo até a próxima mexida.
/// </summary>
public sealed class StatModifier
{
    public readonly ShipStat Stat;

    /// <summary>Bônus fixo, somado direto.</summary>
    public readonly float Add;

    /// <summary>
    /// Fator da base. **Não multiplica o resultado** — vira bônus: 1,5 acrescenta
    /// meia base, 0,5 tira meia base. 1 é neutro.
    /// </summary>
    public readonly float Multiply;

    StatModifier(ShipStat stat, float add, float multiply)
    {
        Stat = stat;
        Add = add;
        Multiply = multiply;
    }

    /// <summary>Soma um tanto. <c>Plus(Acceleration, 2)</c> é "+2 de aceleração".</summary>
    public static StatModifier Plus(ShipStat stat, float amount) =>
        new StatModifier(stat, amount, 1f);

    /// <summary>
    /// Acrescenta um fator da base. <c>Times(AttackSpeed, 2)</c> é "mais uma base
    /// inteira de cadência"; <c>Times(..., 0.5f)</c> tira meia base.
    /// </summary>
    public static StatModifier Times(ShipStat stat, float factor) =>
        new StatModifier(stat, 0f, Mathf.Max(0f, factor));

    public override string ToString() =>
        Add != 0f && Multiply != 1f ? $"{Stat} +{Add} ×{Multiply}"
        : Add != 0f ? $"{Stat} +{Add}"
        : $"{Stat} ×{Multiply}";
}
