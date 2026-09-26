using UnityEngine;

/// <summary>
/// **Reforço estrutural** — o poder da Predadora. Cada obstáculo destruído a
/// tiro é uma carga: soma 2% em aceleração, dano e cadência, até 40% (20
/// cargas), e vale até o fim da corrida.
///
/// **A velocidade de cruzeiro ficou de fora** *(pedido do Raffael, 26/09/2026)*.
/// Na primeira versão ela subia junto, e +40% de cruzeiro aproximava demais a
/// nave da dobra só por ela caçar.
///
/// **A defesa sobe em pontos, e não em fração da base** *(decidido pelo Raffael
/// em 26/09/2026)*. Toda nave tem defesa 0%, e 2% de zero é zero: pela regra
/// dos outros atributos, a defesa nunca subiria. Aqui cada carga soma 2 pontos —
/// 0% → 2% → 4% … até 40%.
///
/// A aceleração maior também faz o abate render mais velocidade, porque o ganho
/// por abate sai dela — ver <see cref="ShipStats"/>. É a mesma regra de
/// qualquer poder que mexa na aceleração.
/// </summary>
[CreateAssetMenu(fileName = "Condicional-ReforcoEstrutural",
                 menuName = "Corrida no Espaço/Poder da nave/Condicional — reforço estrutural")]
public class StructuralReinforcementConditional : ShipConditional
{
    [Header("Efeito")]
    [Tooltip("Quanto cada carga soma, em %. Em aceleração, dano e cadência é fração da base; na " +
             "defesa, pontos de defesa.")]
    [Min(0f)] public float percentPerKill = 2f;

    [Tooltip("O teto do bônus, em %. Com 2 por carga, 40 são 20 cargas.")]
    [Min(0f)] public float maxPercent = 40f;

    [Header("Visual")]
    [Tooltip("Com quantas cargas a nave muda de visual. Cada número alcançado é um estágio a mais: " +
             "com 10 e 20, a nave tem o visual de largada, o do meio e o final.")]
    public int[] stageCounts = { 10, 20 };

    public override int MaxCount =>
        percentPerKill > 0f ? Mathf.CeilToInt(maxPercent / percentPerKill) : 0;

    public override int Stage(ActiveShipConditional active)
    {
        int stage = 0;
        if (stageCounts == null)
            return stage;

        for (int i = 0; i < stageCounts.Length; i++)
        {
            if (active.Count >= stageCounts[i])
                stage++;
        }

        return stage;
    }

    public override void OnObstacleDestroyed(ActiveShipConditional active, ObstacleStats obstacle)
    {
        if (active.Count >= MaxCount)
            return;

        active.Count++;
        float percent = Mathf.Min(active.Count * percentPerKill, maxPercent);
        float factor = 1f + percent / 100f;

        // Troca o degrau inteiro em vez de empilhar quatro modificadores por carga:
        // o resultado é o mesmo, porque todo bônus mede contra a base, e a nave
        // fica com quatro itens na lista em vez de oitenta.
        active.RemoveModifiers();
        active.ApplyModifier(StatModifier.Times(ShipStat.Acceleration, factor));
        active.ApplyModifier(StatModifier.Times(ShipStat.Damage, factor));
        active.ApplyModifier(StatModifier.Times(ShipStat.AttackSpeed, factor));
        active.ApplyModifier(StatModifier.Plus(ShipStat.DefensePercent, percent));
    }
}
