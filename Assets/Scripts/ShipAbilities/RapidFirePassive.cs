using UnityEngine;

/// <summary>
/// **Gatilho rápido** — a passiva da Nay, no nível 60 *(pedido do Raffael,
/// 01/11/2026)*. Mais cadência a corrida inteira. O bônus mede contra a cadência
/// de fábrica, como todo poder, e soma ao da evolução em vez de multiplicá-lo.
/// </summary>
[CreateAssetMenu(fileName = "Passiva-GatilhoRapido",
                 menuName = "Corrida no Espaço/Passiva da nave/Gatilho rápido")]
public class RapidFirePassive : ShipPassive
{
    [Header("Efeito")]
    [Tooltip("Quanto da cadência de BASE ela soma. 0,25 = +25%.")]
    [Min(0f)] public float extraAttackSpeed = 0.25f;

    public override void OnRaceStarted(ActiveShipPassive active) =>
        active.ApplyModifier(StatModifier.Times(ShipStat.AttackSpeed, 1f + extraAttackSpeed));
}
