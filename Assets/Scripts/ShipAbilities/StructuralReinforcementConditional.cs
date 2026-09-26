using UnityEngine;

/// <summary>
/// **Reforço estrutural** — o poder da Predadora. Cada obstáculo destruído a
/// tiro é uma carga: soma 4% em aceleração, dano e cadência, até 40% (10
/// cargas), e vale até o fim da corrida. *(Eram 2% e 20 cargas; o Raffael
/// reduziu para 10 cargas em 26/09/2026, mantendo o teto.)*
///
/// **A velocidade de cruzeiro ficou de fora** *(pedido do Raffael, 26/09/2026)*.
/// Na primeira versão ela subia junto, e +40% de cruzeiro aproximava demais a
/// nave da dobra só por ela caçar.
///
/// **A defesa sobe em pontos, e não em fração da base** *(decidido pelo Raffael
/// em 26/09/2026)*. Toda nave tem defesa 0%, e 4% de zero é zero: pela regra
/// dos outros atributos, a defesa nunca subiria. Aqui cada carga soma 4 pontos —
/// 0% → 4% → 8% … até 40%.
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
    [Min(0f)] public float percentPerKill = 4f;

    [Tooltip("O teto do bônus, em %. Com 4 por carga, 40 são 10 cargas.")]
    [Min(0f)] public float maxPercent = 40f;

    [Header("Visual")]
    [Tooltip("Com quantas cargas a nave muda de visual. Cada número alcançado é um estágio a mais: " +
             "com 5 e 10, a nave tem o visual de largada, o do meio e o final.")]
    public int[] stageCounts = { 5, 10 };

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
        // fica com quatro itens na lista em vez de quarenta.
        active.RemoveModifiers();
        active.ApplyModifier(StatModifier.Times(ShipStat.Acceleration, factor));
        active.ApplyModifier(StatModifier.Times(ShipStat.Damage, factor));
        active.ApplyModifier(StatModifier.Times(ShipStat.AttackSpeed, factor));
        active.ApplyModifier(StatModifier.Plus(ShipStat.DefensePercent, percent));
    }
}
