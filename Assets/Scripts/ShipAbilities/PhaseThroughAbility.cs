using UnityEngine;

/// <summary>
/// **Atravessar** — o poder do Espreitador Espacial *(nave pedida pelo Raffael
/// em 17/10/2026, desenho do Gabriel)*. Por alguns segundos a nave fica
/// translúcida e passa por dentro dos obstáculos; quando o efeito acaba, ela
/// perde um pouco de velocidade.
///
/// **O preço vem no fim, e não no começo,** porque é o fim que o jogador sente:
/// sair do atravessar e já estar um pouco mais devagar é o lembrete de que
/// aquilo foi um atalho.
///
/// Quem faz a nave atravessar é o traço <see cref="ShipTrait.Phasing"/>, que o
/// obstáculo e o estilhaço consultam. Este arquivo só liga o traço e cobra a
/// velocidade.
/// </summary>
[CreateAssetMenu(fileName = "Poder-Atravessar",
                 menuName = "Corrida no Espaço/Poder da nave/Atravessar")]
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
