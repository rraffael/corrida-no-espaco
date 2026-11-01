using UnityEngine;

/// <summary>
/// **Reforço estrutural** — o poder da Predadora. Cada obstáculo destruído a
/// tiro dá cargas, e cada carga soma 1% em aceleração, dano e cadência, e vale
/// até o fim da corrida. *(De fábrica, 1 carga por abate e no máximo 10; no nível
/// 15, 2 por abate; no nível 40, máximo de 20 — pedido do Raffael, 01/11/2026.
/// Antes eram 2% por abate até 20% de saída, o que hoje só vale do nível 40 em
/// diante.)*
///
/// **A velocidade de cruzeiro ficou de fora** *(pedido do Raffael, 26/09/2026)*.
/// Na primeira versão ela subia junto, e +40% de cruzeiro aproximava demais a
/// nave da dobra só por ela caçar.
///
/// **A defesa sobe em pontos, e não em fração da base** *(decidido pelo Raffael
/// em 26/09/2026)*. Toda nave tem defesa 0%, e 1% de zero é zero: pela regra
/// dos outros atributos, a defesa nunca subiria. Aqui cada carga soma 1 ponto.
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
             "defesa, pontos de defesa. O nome é antigo: quando foi criado, um abate era uma carga.")]
    [Min(0f)] public float percentPerKill = 1f;

    [Tooltip("Quantas cargas cada abate dá. Campo próprio porque dobrar o % acima dobraria o valor " +
             "da carga e cortaria o número de cargas pela metade.")]
    [Min(1)] public int chargesPerKill = 1;

    [Tooltip("O teto do bônus, em %. Com 1 por carga, 20 são 20 cargas.")]
    [Min(0f)] public float maxPercent = 10f;

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

        active.Count = Mathf.Min(active.Count + chargesPerKill, MaxCount);
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
