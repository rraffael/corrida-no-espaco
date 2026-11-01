using UnityEngine;

/// <summary>
/// **Debilitante — Desabilitar Armas.** A nave para de atirar por alguns
/// segundos.
///
/// **Desliga a arma de verdade** *(decisão do Raffael, 01/11/2026)*. Até então era
/// um −100% de cadência, mas ele conta sobre a base de fábrica: o bônus da
/// evolução sobrava, e uma nave no nível 60 continuava atirando. Agora zera a
/// cadência, seja qual for o bônus — e por isso também ganha de um Reforço pego
/// junto, o que substitui a escolha de 30/08/2026.
///
/// Quem faz a arma calar é a própria <see cref="ShipWeapon"/>, que não dispara com
/// cadência zero.
///
/// **Não é injusto porque toda fase é vencível sem atirar** — regra de projeto de
/// 07/08/2026, conferida no aparelho. Ficar sem arma custa velocidade e ritmo,
/// nunca a corrida.
/// </summary>
[CreateAssetMenu(fileName = "Debilitante-ArmasDesativadas",
                 menuName = "Corrida no Espaço/Modificador de fase/Debilitante — armas")]
public class WeaponsOffline : LevelModifier
{
    [Header("Efeito")]
    [Tooltip("Quanto da cadência de BASE ele tira. 1 = a arma cala, seja qual for o bônus da nave. " +
             "0,5 seria metade da cadência, para um Debilitante mais brando.")]
    [Range(0f, 1f)] public float attackSpeedLoss = 1f;

    public override void OnGained(ActiveLevelModifier active) =>
        active.ApplyModifier(attackSpeedLoss >= 1f
            ? StatModifier.Zero(ShipStat.AttackSpeed)
            : StatModifier.Times(ShipStat.AttackSpeed, 1f - attackSpeedLoss));
}
