using UnityEngine;

/// <summary>
/// **Intangibilidade** — o poder do Espreitador Espacial *(nave pedida pelo Raffael
/// em 17/10/2026, desenho do Gabriel; o nome é de 18/10, antes era "Atravessar")*.
/// Por alguns segundos a nave fica translúcida e passa por dentro dos
/// obstáculos; quando o efeito acaba, ela perde um pouco de velocidade.
///
/// Acabar com a nave dentro de um obstáculo não bate: há uma carência até ela
/// sair dele — ver <see cref="Obstacle"/>. No nível 60, a passiva
/// <see cref="PerfectIntangibilityPassive"/> a deixa imune ao resto também.
///
/// **O preço vem no fim, e não no começo,** porque é o fim que o jogador sente:
/// sair da intangibilidade e já estar um pouco mais devagar é o lembrete de que
/// aquilo foi um atalho.
///
/// Quem faz a nave atravessar é o traço <see cref="ShipTrait.Phasing"/>, que o
/// obstáculo e o estilhaço consultam. Este arquivo só liga o traço e cobra a
/// velocidade.
/// </summary>
[CreateAssetMenu(fileName = "Poder-Intangibilidade",
                 menuName = "Corrida no Espaço/Poder da nave/Intangibilidade")]
public class PhaseThroughAbility : ShipAbility
{
    [Tooltip("Velocidade que a corrida perde quando o efeito acaba, em unidades internas. " +
             "O painel mostra dez vezes isto: 0,5 aparece como 5 un/s.")]
    [Min(0f)] public float speedLossAtEnd = 0.5f;

    public override void OnActivated(ActiveShipAbility active) =>
        active.ApplyTrait(ShipTrait.Phasing);

    public override void OnEnded(ActiveShipAbility active)
    {
        // O OnEnded também roda quando a corrida acaba com o efeito ligado: aí
        // não há mais velocidade para cobrar.
        var director = RaceDirector.Instance;
        if (director == null || !director.IsRunning)
            return;

        if (RaceSpeed.Instance != null && speedLossAtEnd > 0f)
            RaceSpeed.Instance.Nudge(-speedLossAtEnd);
    }
}
