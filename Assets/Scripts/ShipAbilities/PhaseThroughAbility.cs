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
/// aquilo foi um atalho. **Só não derruba a nave da dobra durante a carga**
/// *(decisão do Raffael, 02/11/2026)*: o poder não pode punir quem o usou para
/// aguentar justamente esse trecho.
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

        var race = RaceSpeed.Instance;
        if (race == null)
            return;

        // A velocidade, e não o IsCharging: este poder roda antes do diretor no
        // quadro, e o aviso de carga chegaria um quadro atrasado.
        bool charging = !director.IsEndless && race.Current >= director.WarpSpeed;
        float loss = LossAtEnd(speedLossAtEnd, race.Target, director.WarpSpeed, charging);
        if (loss > 0f)
            race.Nudge(-loss);
    }

    /// <summary>
    /// Quanto cobrar. Na carga, só o que sobra acima da dobra: o piso segura a queda,
    /// mas nunca sobe o alvo, senão acabar o poder daria velocidade.
    /// </summary>
    public static float LossAtEnd(float loss, float target, float warpSpeed, bool charging) =>
        charging ? Mathf.Clamp(target - warpSpeed, 0f, loss) : loss;
}
