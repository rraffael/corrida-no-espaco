using UnityEngine;

/// <summary>
/// Um **jeito de jogar**, para o simulador de corridas: quanto o piloto demora
/// para reagir, até onde ele enxerga, quanto erra, se caça obstáculo e como usa
/// o poder.
///
/// **Para que servem** *(pedido do Raffael, 18/10/2026)*: cinco personas, do
/// muito ruim ao perfeito. O número de uma persona sozinha diz pouco — o
/// "Novato" perder a Fase 3 não é notícia. O que mede é **a mesma persona em
/// duas circunstâncias**: a nave nova contra a antiga, o obstáculo novo contra a
/// fase sem ele, o modificador antes e depois do ajuste. E as cinco juntas
/// mostram para quem a mudança pesa — às vezes ela só aperta para quem já
/// jogava mal.
///
/// O cérebro é o da Super IA (<see cref="LaneSense"/>); a persona só diz com
/// que lentidão e com que descuido ele é usado. Ver <see cref="SimulatedPilot"/>.
/// </summary>
[CreateAssetMenu(fileName = "Persona", menuName = "Corrida no Espaço/Simulador/Persona de piloto")]
public class PilotPersona : ScriptableObject
{
    public enum PowerHabit
    {
        /// <summary>Esquece que tem poder.</summary>
        Nunca,

        /// <summary>Aperta em qualquer momento, sem motivo — em média uma vez a cada 12 s com o poder pronto.</summary>
        AToa,

        /// <summary>Só quando se vê encurralado: perigo perto e nenhuma saída.</summary>
        NoAperto,

        /// <summary>No aperto, no fim da dobra quando o perigo chega, e à vontade se o poder recarrega.</summary>
        Esperto,
    }

    [Tooltip("Nome que aparece no relatório.")]
    public string displayName = "Persona";

    [TextArea(2, 5)]
    public string description;

    [Tooltip("Ordem de habilidade: 1 é o pior. O relatório lista nesta ordem.")]
    [Min(1)] public int skillRank = 1;

    [Header("Reflexo")]
    [Tooltip("Segundos entre decidir e o dedo agir. O mundo continua andando nesse meio-tempo, e a " +
             "decisão não é refeita: é o atraso de gente.")]
    [Min(0f)] public float reactionSeconds = 0.2f;

    [Tooltip("De quanto em quanto tempo ele olha a pista de novo. Zero: a cada quadro.")]
    [Min(0f)] public float thinkInterval = 0.1f;

    [Tooltip("Tempo entre um arraste e o seguinte, para cruzar mais de uma faixa. O jogador troca " +
             "uma faixa por arraste. Zero: cruza de uma vez, como a Super IA.")]
    [Min(0f)] public float swipeInterval = 0.15f;

    [Header("Vista")]
    [Tooltip("A que distância o obstáculo na faixa dele vira motivo para sair. A Super IA usa 5.")]
    [Min(0.5f)] public float dangerDistance = 5f;

    [Tooltip("Folga que ele quer na faixa de destino. A Super IA usa 7.")]
    [Min(0.5f)] public float clearDistance = 7f;

    [Tooltip("Até onde ele enxerga. A Super IA usa 14.")]
    [Min(1f)] public float lookAhead = 14f;

    [Header("Erro")]
    [Tooltip("Chance, a cada decisão de desviar, de errar: metade das vezes ele congela por um " +
             "instante, metade foge para uma faixa vizinha qualquer. Com a pista calma, a mesma " +
             "chance o faz trocar de faixa à toa de vez em quando.")]
    [Range(0f, 1f)] public float mistakeChance = 0.05f;

    [Header("Estilo")]
    [Tooltip("Vontade de caçar: chance, a cada meio segundo de pista calma, de ir para a faixa de um " +
             "obstáculo que dá tempo de destruir. É o que transforma abate em velocidade.")]
    [Range(0f, 1f)] public float huntChance = 0.5f;

    [Tooltip("Prefere a faixa com Reforço ou Especial, entre faixas igualmente seguras.")]
    public bool seeksRewards = true;

    [Tooltip("Desvia do Debilitante. Desligado: nem repara nele.")]
    public bool avoidsDebuffs = true;

    public PowerHabit powerHabit = PowerHabit.NoAperto;
}
