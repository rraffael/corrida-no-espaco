using UnityEngine;

/// <summary>
/// **Debilitante — Desabilitar Armas.** A nave para de atirar por alguns
/// segundos.
///
/// **É um −100% de cadência, e não um interruptor** *(escolha de 30/08/2026)*.
/// Os dois dariam o mesmo resultado sozinhos; a diferença aparece quando outro
/// modificador estiver valendo junto. Como número, ele obedece à regra de
/// empilhamento do jogo: pegar um Reforço de +100% com as armas desabilitadas
/// devolve a cadência de fábrica, que é o que qualquer jogador esperaria de dois
/// efeitos opostos. Como interruptor, o "desligado" ganharia do bônus e o
/// Reforço pareceria quebrado.
///
/// Quem faz a arma calar de verdade é a própria <see cref="ShipWeapon"/>, que não
/// dispara com cadência zero — o piso do atributo foi baixado a zero no mesmo dia,
/// justamente para este caso existir.
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
    [Tooltip("Quanto da cadência de BASE ele tira. 1 = -100%, ou seja, a arma cala. " +
             "0,5 seria metade da cadência, para um Debilitante mais brando.")]
    [Range(0f, 1f)] public float attackSpeedLoss = 1f;

    public override void OnGained(ActiveLevelModifier active) =>
        active.ApplyModifier(StatModifier.Times(ShipStat.AttackSpeed, 1f - attackSpeedLoss));
}
