using System;

/// <summary>
/// A sorte da pista, com um dado separado para cada tipo de sorteio.
///
/// **Por que separar:** com um dado só, qualquer sorteio a mais ou a menos
/// desalinhava todos os seguintes. Aqui, o que um sistema sorteia não muda o que
/// outro vai tirar, e o simulador compara naves em pistas mais parecidas.
///
/// **No jogo de verdade, cada corrida tem sorte nova**, sem ligação com o
/// salvamento ou a data. A pista só é fixa quando o simulador pede.
///
/// **A semente da pista não considera nave, nível nem persona**, de propósito: é
/// isso que põe todas na mesma largada. Fase e dificuldade entram, para que cada
/// uma tenha pistas próprias em vez de reaproveitar a mesma sequência.
/// </summary>
public static class RaceLuck
{
    public enum Stream
    {
        ObstacleEntry,
        ObstacleTiming,
        ObstacleType,
        ObstacleLane,
        ModifierTiming,
        ModifierPick,
        ModifierLane,
    }

    static readonly Random[] streams = new Random[Enum.GetValues(typeof(Stream)).Length];

    static bool isFixed;
    static int fixedSeed;
    static string fixedLevel;
    static int fixedDifficulty;

    /// <summary>Fixa a pista das próximas corridas. Só o simulador chama.</summary>
    public static void UseFixedTrack(int seed, string level, int difficulty)
    {
        isFixed = true;
        fixedSeed = seed;
        fixedLevel = level;
        fixedDifficulty = difficulty;
        NewRace();
    }

    /// <summary>Volta à sorte normal do jogo.</summary>
    public static void UseFreeTrack()
    {
        isFixed = false;
        fixedLevel = null;
        NewRace();
    }

    /// <summary>Corrida nova: cada dado recomeça no próximo sorteio.</summary>
    public static void NewRace() => Array.Clear(streams, 0, streams.Length);

    public static float Range(Stream stream, float min, float max) =>
        min + (float)(Get(stream).NextDouble() * (max - min));

    /// <summary>Inteiro de <paramref name="min"/> até <paramref name="maxExclusive"/>, sem incluí-lo.</summary>
    public static int Range(Stream stream, int min, int maxExclusive) =>
        Get(stream).Next(min, maxExclusive);

    /// <summary>
    /// A semente de <paramref name="system"/> na pista fixa. Hash próprio, porque o
    /// <see cref="string.GetHashCode()"/> não promete o mesmo valor entre execuções,
    /// e somar números daria sementes parecidas para fases vizinhas.
    /// </summary>
    public static int TrackSeed(string system)
    {
        unchecked
        {
            uint hash = 2166136261u;
            hash = Mix(hash, fixedSeed);
            hash = Mix(hash, fixedDifficulty);
            hash = Mix(hash, fixedLevel);
            hash = Mix(hash, system);

            // Espalha os bits no fim: sementes vizinhas não podem dar dados parecidos.
            hash ^= hash >> 16;
            hash *= 0x85ebca6bu;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35u;
            hash ^= hash >> 16;

            return (int)(hash & 0x7fffffffu);
        }
    }

    static Random Get(Stream stream)
    {
        int index = (int)stream;
        if (streams[index] == null)
        {
            int seed = isFixed ? TrackSeed(stream.ToString()) : Guid.NewGuid().GetHashCode();
            streams[index] = new Random(seed);
        }

        return streams[index];
    }

    static uint Mix(uint hash, int value)
    {
        unchecked
        {
            for (int shift = 0; shift < 32; shift += 8)
                hash = (hash ^ ((uint)value >> shift & 0xffu)) * 16777619u;

            return hash;
        }
    }

    static uint Mix(uint hash, string value)
    {
        unchecked
        {
            if (value != null)
            {
                foreach (char c in value)
                    hash = (hash ^ c) * 16777619u;
            }

            // O separador impede que "ab" + "c" dê o mesmo que "a" + "bc".
            return (hash ^ 0xffu) * 16777619u;
        }
    }

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad() => UseFreeTrack();
}
