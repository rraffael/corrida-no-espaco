using UnityEngine;

/// <summary>
/// **Frenesi** — a passiva da Predadora, no nível 60 *(pedido do Raffael,
/// 01/11/2026)*. Abates em sequência dão cargas temporárias, por cima do Reforço
/// estrutural; passar a janela sem abater perde **todas** elas de uma vez.
///
/// **É para premiar a caça em sequência**, e não só a caça. As cargas do Reforço
/// ficam até o fim da corrida; estas pedem que o jogador continue abatendo.
///
/// **Não encosta nas cargas do Reforço:** os dois têm modificadores separados, e
/// nenhum sabe do outro. Como todo bônus mede contra a base, os dois se somam.
///
/// **O relógio anda com o tempo do jogo**: parado na pausa, e mais lento junto com
/// a corrida no Tempo Lento.
/// </summary>
[CreateAssetMenu(fileName = "Passiva-Frenesi",
                 menuName = "Corrida no Espaço/Passiva da nave/Frenesi")]
public class FrenzyPassive : ShipPassive
{
    [Header("Efeito")]
    [Tooltip("Cargas temporárias que cada abate soma.")]
    [Min(1)] public int chargesPerKill = 2;

    [Tooltip("O máximo de cargas temporárias.")]
    [Min(1)] public int maxCharges = 10;

    [Tooltip("Quanto cada carga soma, em %. Em aceleração, dano e cadência é fração da base; na " +
             "defesa, pontos de defesa.")]
    [Min(0f)] public float percentPerCharge = 1f;

    [Tooltip("Segundos sem abater até perder todas as cargas temporárias. Cada abate renova.")]
    [Min(0.1f)] public float windowSeconds = 4f;

    public override void OnObstacleDestroyed(ActiveShipPassive active, ObstacleStats obstacle)
    {
        active.SecondsLeft = windowSeconds;

        int charges = Mathf.Min(active.Count + chargesPerKill, maxCharges);
        if (charges == active.Count)
            return;

        active.Count = charges;
        Apply(active);
    }

    public override void OnTick(ActiveShipPassive active, float deltaTime)
    {
        if (active.Count == 0)
            return;

        active.SecondsLeft -= deltaTime;
        if (active.SecondsLeft > 0f)
            return;

        active.Count = 0;
        active.RemoveModifiers();
    }

    public override string HudBonus(ActiveShipPassive active) =>
        active.Count > 0 ? $"+{active.Count}" : string.Empty;

    /// <summary>Troca o degrau inteiro, pelo mesmo motivo do Reforço estrutural.</summary>
    void Apply(ActiveShipPassive active)
    {
        float percent = active.Count * percentPerCharge;
        float factor = 1f + percent / 100f;

        active.RemoveModifiers();
        active.ApplyModifier(StatModifier.Times(ShipStat.Acceleration, factor));
        active.ApplyModifier(StatModifier.Times(ShipStat.Damage, factor));
        active.ApplyModifier(StatModifier.Times(ShipStat.AttackSpeed, factor));
        active.ApplyModifier(StatModifier.Plus(ShipStat.DefensePercent, percent));
    }
}
