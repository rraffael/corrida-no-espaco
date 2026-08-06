using System;
using UnityEngine;

/// <summary>
/// Quando um tipo de obstáculo entra em cena, dentro de uma corrida. A entrada é
/// sorteada entre <see cref="earliestSeconds"/> e <see cref="latestSeconds"/>,
/// então duas partidas da mesma fase não ficam idênticas.
/// </summary>
[Serializable]
public class ObstacleSchedule
{
    public ObstacleStats stats;

    [Tooltip("Segundo mais cedo em que este tipo pode começar a aparecer. 0 = desde o início.")]
    [Min(0f)] public float earliestSeconds;

    [Tooltip("Segundo mais tarde. Igual ao mais cedo: entra sempre na mesma hora.")]
    [Min(0f)] public float latestSeconds;

    [Tooltip("Peso no sorteio depois de liberado. 2 aparece o dobro de um de peso 1.")]
    [Min(0.01f)] public float weight = 1f;
}

/// <summary>
/// A ficha da fase: tudo o que faz a fase 2 ser diferente da fase 1. Antes disto,
/// as regras viviam espalhadas entre o RaceDirector e o spawner, e criar uma fase
/// nova exigiria mexer em código.
/// </summary>
[CreateAssetMenu(fileName = "Fase", menuName = "Corrida no Espaço/Ficha de fase")]
public class LevelDefinition : ScriptableObject
{
    [Header("Identidade")]
    public string displayName = "Fase 1";

    [Tooltip("Ordem no menu. A fase N só destrava vencendo a N-1.")]
    [Min(1)] public int order = 1;

    [Header("Dobra")]
    [Tooltip("Velocidade que começa a carregar a dobra. A dificuldade soma em cima disto.")]
    [Min(1f)] public float warpSpeed = 15f;

    [Tooltip("Segundos segurando a velocidade para a dobra completar.")]
    [Min(0f)] public float warpChargeSeconds = 5f;

    [Header("Ritmo")]
    [Tooltip("Segundos entre obstáculos no começo da corrida.")]
    [Min(0.2f)] public float startInterval = 1.4f;

    [Tooltip("Segundos entre obstáculos depois da rampa. Menor que o inicial = aperta com o tempo.")]
    [Min(0.2f)] public float endInterval = 0.9f;

    [Tooltip("Em quantos segundos o ritmo vai do inicial ao final.")]
    [Min(1f)] public float rampSeconds = 60f;

    [Tooltip("Variação sorteada no intervalo, para o ritmo não virar metrônomo.")]
    [Min(0f)] public float intervalJitter = 0.3f;

    [Header("Obstáculos")]
    public ObstacleSchedule[] obstacles = Array.Empty<ObstacleSchedule>();

    [Header("Modo")]
    [Tooltip("Fase sem fim: não tem dobra, e a pontuação é quanto tempo a nave aguenta. " +
             "É a única que alimenta o leaderboard.")]
    public bool endless;

    /// <summary>Intervalo entre obstáculos no segundo indicado da corrida.</summary>
    public float IntervalAt(float elapsedSeconds)
    {
        float t = rampSeconds > 0f ? Mathf.Clamp01(elapsedSeconds / rampSeconds) : 1f;
        return Mathf.Lerp(startInterval, endInterval, t);
    }
}
