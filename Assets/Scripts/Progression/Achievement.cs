using System;
using UnityEngine;

/// <summary>O número de carreira que uma conquista acompanha. Ver <see cref="CareerData"/>.</summary>
public enum CareerCounter
{
    /// <summary>Corridas que chegaram ao fim.</summary>
    Races,

    /// <summary>Fases vencidas — dobras completas.</summary>
    Wins,

    /// <summary>Corridas da fase sem fim encerradas.</summary>
    EndlessRuns,

    /// <summary>Obstáculos destruídos a tiro.</summary>
    Kills,

    /// <summary>Distância somada de todas as corridas, em "un" — o número que a tela mostra.</summary>
    Distance,

    /// <summary>Maior distância numa corrida da fase sem fim, em "un".</summary>
    BestEndlessDistance,

    /// <summary>
    /// Vezes que o jogador evoluiu uma nave, pagando — os botões de teste não
    /// contam. Entrou em 26/09/2026 para a conquista "evolua uma nave pela
    /// primeira vez". Sempre no fim da lista: o número de cada contador é o que
    /// fica gravado na ficha.
    /// </summary>
    ShipLevelUps,
}

/// <summary>
/// A ficha de uma **conquista** *(estrutura de 26/09/2026; conteúdo do Raffael)*:
/// um contador de carreira, a meta, e quanto de moeda ela paga ao ser resgatada.
/// Cada uma é um arquivo em <c>Assets/Conquistas/</c> e entra no
/// <see cref="AchievementCatalog"/>.
///
/// **É ficha de dados, e não classe por conquista** — ao contrário dos poderes.
/// Conquista não tem efeito no jogo: é "contou até N, paga X". O que varia é o
/// número, e número mora em ficha. Conquista que precise de uma condição nova
/// ganha um contador novo em <see cref="CareerCounter"/> e em
/// <see cref="CareerStats"/>, e todas as outras continuam iguais.
/// </summary>
[CreateAssetMenu(fileName = "Conquista", menuName = "Corrida no Espaço/Conquista")]
public class Achievement : ScriptableObject
{
    /// <summary>
    /// Quando o jogador pode ver a conquista *(desenho do Raffael, 26/09/2026)*.
    /// Escondida aparece como "????????", mas **sempre com a recompensa** — é o
    /// que dá vontade de descobrir.
    /// </summary>
    public enum Visibility
    {
        /// <summary>Aparece inteira desde o começo.</summary>
        Visivel,

        /// <summary>Escondida até outra conquista — o requisito — ser completada.</summary>
        OcultaAteRequisito,

        /// <summary>Escondida até ela mesma ser completada. A surpresa.</summary>
        Secreta,
    }

    [Header("Identidade")]
    public string displayName = "Conquista";

    [Tooltip("O que é preciso fazer, na linguagem do jogador.")]
    [TextArea(1, 3)] public string description = "";

    [Header("Visibilidade")]
    public Visibility visibility = Visibility.Visivel;

    [Tooltip("Só vale com OcultaAteRequisito: a conquista que, completada, revela esta. Vazio " +
             "faz ela se comportar como Secreta.")]
    public Achievement requirement;

    [Header("Meta")]
    public CareerCounter counter = CareerCounter.Kills;

    [Tooltip("Quanto o contador precisa chegar. Distância é em \"un\", o número da tela.")]
    [Min(1f)] public float target = 100f;

    [Tooltip("Só conta o que foi feito COM esta nave. Vazio: qualquer nave.\n\n" +
             "Vale para corridas, vitórias, abates, distância e evoluções. Os contadores da fase sem fim " +
             "não são separados por nave, e ignoram este campo.")]
    public ShipDefinition ship;

    [Header("Recompensa")]
    [Tooltip("Quanto da moeda do jogo ela paga ao ser resgatada.")]
    [Min(0)] public int reward = 0;

    [Header("Play Games — Fase 7")]
    [Tooltip("Id da conquista espelhada no Play Games, quando existir. Vazio: não espelha.")]
    public string playGamesId = "";
}
